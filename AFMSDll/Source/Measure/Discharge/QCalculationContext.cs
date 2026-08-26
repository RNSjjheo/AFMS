namespace AFMSDll
{
    /// <summary>
    /// 유량 산정 객체가 사용하는 장비, 산정법, 단면 및 측선 설정입니다.
    /// </summary>


    /// <summary>
    /// 유량 산정에 사용할 원시 수위·유속 자료와 수신 상태입니다.
    /// </summary>

    /// <summary>
    /// 현재 산정 슬롯과 산정 과정에서 생성되는 중간값 및 결과입니다.
    /// </summary>
    public sealed class QCalculationContext
    {
        public int SlotId { get; internal set; } = -1;
        public DateOnly SlotDate { get; internal set; }
        public TimeOnly SlotTime { get; internal set; }
        public double CrossSectionArea { get; set; }
        public double Value { get; set; }
        public double Uncertainty { get; set; }

        internal void ClearSlot()
        {
            SlotId = -1;
            SlotDate = default;
            SlotTime = default;
            CrossSectionArea = 0.0;
            Value = 0.0;
            Uncertainty = 0.0;
        }
    }
}
