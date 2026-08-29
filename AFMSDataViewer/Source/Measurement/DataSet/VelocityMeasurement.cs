namespace AFMSDataViewer
{
    /// <summary>
    /// 유속계 종류와 무관하게 MeasurementDataHub가 보관하는 유속 측정 원형입니다.
    /// 장비별 원본 테이블 차이는 자식 형식과 전용 데이터소스에서 처리합니다.
    /// </summary>
    public abstract class VelocityMeasurement : IRealtimeMeasurement
    {
        protected VelocityMeasurement(DateTime time, string deviceKey, IEnumerable<VelocityTransectMeasurement> transects)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(deviceKey);
            ArgumentNullException.ThrowIfNull(transects);

            VelocityTransectMeasurement[] values = transects.OrderBy(transect => transect.TransectNo).ToArray();
            ValidateTransects(values);

            Time = time;
            DeviceKey = deviceKey.Trim();
            Transects = values;
        }

        public DateTime Time { get; }

        public string DeviceKey { get; }

        public abstract string SourceType { get; }

        public IReadOnlyList<VelocityTransectMeasurement> Transects { get; }

        private static void ValidateTransects(IReadOnlyList<VelocityTransectMeasurement> transects)
        {
            HashSet<int> transectNumbers = [];
            foreach (VelocityTransectMeasurement transect in transects)
            {
                ArgumentNullException.ThrowIfNull(transect);
                if (transect.TransectNo <= 0)
                    throw new ArgumentOutOfRangeException(nameof(transects), "측선 번호는 1 이상이어야 합니다.");
                if (!transectNumbers.Add(transect.TransectNo))
                    throw new ArgumentException($"{transect.TransectNo}번 측선이 중복되었습니다.", nameof(transects));
                if (!double.IsFinite(transect.Velocity) || !double.IsFinite(transect.Uncertainty))
                    throw new ArgumentException("유속과 불확도는 유한한 값이어야 합니다.", nameof(transects));
            }
        }
    }
}
