namespace AFMSDataViewer
{
    /// <summary>VIDEO 유속계에서 조회한 측선별 측정 자료입니다.</summary>
    public sealed class VideoVelocityMeasurement : VelocityMeasurement
    {
        public const string SourceName = "VIDEO";

        public VideoVelocityMeasurement(
            DateTime time,
            string deviceKey,
            IEnumerable<VelocityTransectMeasurement> transects,
            int deviceId = 0,
            int deviceNo = 0,
            string? meterType = null)
            : base(time, deviceKey, transects, deviceId, deviceNo, meterType)
        {
        }

        public override string SourceType => SourceName;
    }
}
