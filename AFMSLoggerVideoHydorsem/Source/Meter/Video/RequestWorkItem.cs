using AFMSDll;

namespace AFMSLoggerVideoHydorsem
{
    public sealed class RequestWorkItem
    {
        public required string Id { get; init; }
        public string Key = string.Empty;
        public required string Message { get; init; }
        public required string Path { get; init; }
        public required ApiMethod Method { get; init; }
        public DateTimeOffset CreatedAt { get; init; }

        public void SetKey()
        {
            Key = Id.Length <= 5 ? Id : Id.Substring(0, 5);
            Key = Key.ToUpper();
        }
    }
}
