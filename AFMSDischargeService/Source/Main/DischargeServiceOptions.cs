namespace AFMSDischargeService
{
    internal sealed class DischargeServiceOptions
    {
        public const string SectionName = "Discharge";

        public DateTime CalculationStartTime { get; set; } =
            new(2026, 8, 20, 0, 0, 0, DateTimeKind.Local);
    }
}
