using Microsoft.Extensions.Options;

namespace AFMSSediService
{
    internal sealed class WorkerSSCProcess : WorkerSSC
    {
        private readonly SscSlotFinder _SlotFinder;
        private readonly SscRepository repository = new();

        public WorkerSSCProcess(ILogger<WorkerSSCProcess> logger, IOptions<SSCServiceOptions> options) : base(logger, options)
        {
            _SlotFinder = new SscSlotFinder(logger);
        }

        protected override Task<int> ProcessBatchAsync(
            RSandProfileSnapshot profile,
            ChannelMasterSource source,
            SedFileWriter fileWriter,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SscCalculationSlot? slot = _SlotFinder.FindNext(source);
            if (slot == null) return Task.FromResult(0);

            ChannelMasterMeasurement measurement = repository.LoadChannelMaster(slot, source);
            double discharge = repository.LoadDischarge(slot);
            SscCalculationResult result = SscCalculator.Calculate(measurement, profile.Device, discharge);
            repository.Save(slot, result);

            Logger.LogInformation(
                "SSC 계산을 완료했습니다. SlotId={SlotId}, Source={Source}, SSC={Ssc}",
                slot.Id,
                source.HeaderTable,
                result.Ssc);

            return Task.FromResult(1);
        }
    }
}
