namespace AFMSExtraLogger
{
    public interface IRequestTaskQueue
    {
        bool TryQueue(RequestWorkItem item);
        ValueTask<RequestWorkItem> DequeueAsync(CancellationToken cancellationToken);
    }
}
