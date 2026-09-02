using Microsoft.Extensions.Options;

namespace AFMSSediService
{
    internal sealed class WorkerSSCProcess : WorkerSSC
    {
        private readonly SscSlotFinder _SlotFinder;

        public WorkerSSCProcess(ILogger<WorkerSSCProcess> logger, IOptions<SSCServiceOptions> options) : base(logger, options)
        {
            _SlotFinder = new SscSlotFinder(logger);
        }

        protected override Task<int> ProcessBatchAsync(RSandProfileSnapshot profile, SedFileWriter fileWriter, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _SlotFinder.FindOldestUnprocessedSlot();
            _SlotFinder.FindOldestVelocityMeasuredSlot(profile);

            return Task.FromResult(0);
        }
    }
}
