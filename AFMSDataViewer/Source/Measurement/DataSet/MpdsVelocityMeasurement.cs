namespace AFMSDataViewer
{
    /// <summary>MPDS 유속계에서 조회한 측선별 측정 자료입니다.</summary>
    public sealed class MpdsVelocityMeasurement : VelocityMeasurement
    {
        public const string SourceName = "MPDS";

        public MpdsVelocityMeasurement(
            DateTime time,
            string deviceKey,
            IEnumerable<VelocityTransectMeasurement> transects)
            : base(time, deviceKey, transects)
        {
        }

        public override string SourceType => SourceName;
    }
}
