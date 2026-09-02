using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSSediService
{
    public sealed class SSCServiceOptions
    {
        public const string SectionName = "SSCSection";

        public DateTime CalculationStartTime { get; set; } = new(2026, 8, 25, 0, 0, 0, DateTimeKind.Local);
        public string DataDirectory { get; set; } = "Data";
        public int BatchSize { get; set; } = 100;
        public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(10);
    }
}
