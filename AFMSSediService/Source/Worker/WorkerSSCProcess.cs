using Microsoft.Extensions.Options;

namespace AFMSSediService
{
    internal sealed class WorkerSSCProcess : WorkerSSC
    {
        private readonly SscRepository repository = new SscRepository();

        public WorkerSSCProcess(ILogger<WorkerSSCProcess> logger, IOptions<SSCServiceOptions> options) : base(logger, options)
        {
        }

        protected override async Task<int> ProcessBatchAsync(RSandProfileSnapshot profile, SedFileWriter fileWriter, CancellationToken cancellationToken)
        {
            IReadOnlyList<SscMeasurementKey> keys = repository.LoadPendingKeys(Options.CalculationStartTime,Options.BatchSize, profile);
            int processed = 0;

            foreach (SscMeasurementKey key in keys)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    repository.MarkInProgress(key);

                    ProcessDevice(key, 1, profile.A);
                    ProcessDevice(key, 2, profile.B);

                    SediSedimentRecord record = repository.LoadSedimentRecord(key, profile);
                    string path = await fileWriter.WriteAsync(record, cancellationToken);

                    repository.MarkCompleted(key);
                    processed++;
                    Logger.LogInformation(
                        "SSC 계산과 SED 저장을 완료했습니다. 측정={MeasureDate} {MeasureTime}, 파일={FilePath}",
                        key.MeasureDate,
                        key.MeasureTime,
                        path);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    TryMarkPending(key);
                    throw;
                }
                catch (Exception ex)
                {
                    TryMarkPending(key);
                    Logger.LogError(
                        ex,
                        "SSC 계산 또는 저장에 실패했습니다. 측정={MeasureDate} {MeasureTime}",
                        key.MeasureDate,
                        key.MeasureTime);
                }
            }

            return processed;
        }

        private void ProcessDevice(
            SscMeasurementKey key,
            int deviceNumber,
            RSandDeviceProfile profile)
        {
            if (!profile.IsEnabled || repository.HasCalculation(key, deviceNumber)) return;

            ChannelMasterMeasurement source = repository.LoadChannelMaster(key, deviceNumber);
            double discharge = repository.LoadDischarge(key);
            SscCalculationResult result = SscCalculator.Calculate(source, profile, discharge);
            repository.SaveCalculation(key, deviceNumber, result);

            Logger.LogInformation(
                "SSC 계산 완료. 측정={MeasureDate} {MeasureTime}, 장비={DeviceNumber}, SSC={Ssc}",
                key.MeasureDate,
                key.MeasureTime,
                deviceNumber,
                result.Ssc);
        }

        private void TryMarkPending(SscMeasurementKey key)
        {
            try
            {
                repository.MarkPending(key);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "SSC 처리 상태 복원에 실패했습니다. 측정={MeasureDate} {MeasureTime}",
                    key.MeasureDate,
                    key.MeasureTime);
            }
        }
    }
}
