using System.Threading.Channels;

namespace AFMSExtraLogger
{
    public sealed class RequestTaskQueue : IRequestTaskQueue
    {
        private readonly Channel<RequestWorkItem> _channel;

        public RequestTaskQueue()
        {
            var options = new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = false,
                SingleWriter = false
            };

            _channel = Channel.CreateBounded<RequestWorkItem>(options);
        }

        public bool TryQueue(RequestWorkItem item)
        {
            return _channel.Writer.TryWrite(item);
        }

        public ValueTask<RequestWorkItem> DequeueAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}
