namespace AFMSDll
{
    public abstract class DischargeBase
    {
        protected DischargeBase(DischargeMethod method)
        {
            Method = method;
        }

        public int Id { get; set; } = -1;
        public int HydroMeterId { get; set; } = -1;
        public DateOnly MeasureDate { get; set; }
        public TimeOnly MeasureTime { get; set; }
        public double Value { get; set; }
        public double Uncertainty { get; set; }
        public DischargeMethod Method { get; }
        public int MethodConfigId { get; set; } = -1;
    }
}
