namespace AFMSDataViewer
{
    /// <summary>측정 슬롯에 속한 수위계 상태입니다.</summary>
    public sealed class WaterLevelGauge
    {
        private readonly MeasurementSlot slot;

        internal WaterLevelGauge(MeasurementSlot slot)
        {
            ArgumentNullException.ThrowIfNull(slot);
            this.slot = slot;
        }

        public DateTime SlotTime => slot.SlotTime;

        public LevelMeasurement? Measurement { get; internal set; }

        public double? Level => Measurement?.Value;

        public bool IsValid => Measurement?.IsValid == true;
    }
}
