using Microsoft.Extensions.Options;

namespace AFMSSediService
{
    internal sealed class WorkerSSCProcess : WorkerSSC
    {
        private readonly SscSlotFinder _SlotFinder;
        private int? lastLoggedSlotId;

        public WorkerSSCProcess(ILogger<WorkerSSCProcess> logger, IOptions<SSCServiceOptions> options) : base(logger, options)
        {
            _SlotFinder = new SscSlotFinder();
        }

        protected override Task<int> ProcessBatchAsync(RSandProfileSnapshot profile, SedFileWriter fileWriter, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SscCalculationSlot? slot = _SlotFinder.FindOldestUnprocessedSlot();

            if (slot != null && lastLoggedSlotId != slot.Id)
            {
                Logger.LogInformation(
                    "다음 SSC 계산 대상 슬롯을 확인했습니다. SlotId={SlotId}, SlotTime={SlotTime:yyyy-MM-dd HH:mm:ss}, 측정={MeasureDate} {MeasureTime}",
                    slot.Id,
                    slot.SlotTime,
                    slot.MeasureDate,
                    slot.MeasureTime);
                lastLoggedSlotId = slot.Id;
            }

            return Task.FromResult(0);
        }
    }
}
