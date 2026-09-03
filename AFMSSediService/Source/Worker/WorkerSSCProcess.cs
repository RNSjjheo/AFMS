using Microsoft.Extensions.Options;

namespace AFMSSediService
{
    internal sealed class WorkerSSCProcess : WorkerSSC
    {
        private readonly SscRepository repository = new();

        public WorkerSSCProcess(ILogger<WorkerSSCProcess> logger, IOptions<SSCServiceOptions> options) : base(logger, options)
        {
        }

        protected override Task<int> ProcessBatchAsync(SSCProfileSnapshot profile, ChannelMasterSource source, SedFileWriter fileWriter, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SscCalculationSlot? slot = _SlotFinder.FindNext(source);
            if (slot == null) return Task.FromResult(0);

            ChannelMasterMeasurement measurement = repository.LoadChannelMaster(slot, source);
            double discharge = repository.LoadDischarge(slot);
            SscCalculationResult result = SscCalculator.Calculate(measurement, profile.Device, discharge);

            Logger.LogInformation($"[SSC] {SscCalculationLogFormatter.Format(slot, source, measurement, profile.Device, result)}");
            Logger.LogInformation($"[SSC] 계산을 완료했습니다. SlotId={slot.Id}, Source={source.HeaderTable}, SSC={result.Ssc}");

            repository.Save(slot, result);

            return Task.FromResult(1);
        }
    }
}
