namespace AFMSDataViewer
{
    /// <summary>측정 슬롯에 속한 전력 장치 상태입니다.</summary>
    public sealed class PowerDevice
    {
        private readonly MeasurementSlot slot;

        internal PowerDevice(MeasurementSlot slot)
        {
            ArgumentNullException.ThrowIfNull(slot);
            this.slot = slot;
        }

        public DateTime SlotTime => slot.SlotTime;

        public VoltageMeasurement? Measurement { get; internal set; }

        public double? InputVoltage => Measurement?.InputVoltage;

        public double? OutputVoltage => Measurement?.OutputVoltage;

        public bool IsInputValid => Measurement?.IsInputValid == true;

        public bool IsOutputValid => Measurement?.IsOutputValid == true;
    }
}
