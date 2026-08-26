namespace AFMSDll
{
    /// <summary>DB에 의존하지 않는 유량 설정·수집·결과 상태입니다.</summary>
    public abstract class _QBase
    {
        public int Id { get; set; } = -1;
        public QConfiguration Configuration { get; }
        public QMeasurementContext Measurement { get; } = new();
        public QCalculationContext Calculation { get; } = new();
        public virtual bool IsImplemented => false;

        protected _QBase(DischargeMethod method)
        {
            Configuration = new QConfiguration(method);
        }
    }
}
