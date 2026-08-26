namespace AFMSDll
{
    public sealed class QMidSection : _QBase
    {
        public TransectCollection Transects => Configuration.CrossSection.Transects;
        public IReadOnlyList<QTransectMeasurement> TransectMeasurements => Measurement.Transects;
        public DiscVerMidSection Version { get; set; }
        public int CellRangeMin { get; set; }
        public int CellRangeMax { get; set; }
        public double ConversionFactor { get; set; }
        public QMidSection() : base(DischargeMethod.MidSection) { }
    }
}
