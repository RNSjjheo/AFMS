using AFMSDll;

namespace AFMSDischargeService
{
    internal static class DischargeLogFormatter
    {
        public static string GetMethodName(DischargeMethod method)
        {
            return method switch
            {
                DischargeMethod.VeloDist => "유속분포법",
                DischargeMethod.MidSection => "중간단면적법",
                DischargeMethod.SurfaceVelo => "지표유속법",
                DischargeMethod.RatingCurve => "수위-유량곡선법",
                _ => method.ToString()
            };
        }

        public static string GetDeviceKey(QConfiguration configuration, QMeasurementContext measurement)
        {
            return $"{configuration.DeviceId}({measurement.DeviceName})";
        }

        public static string GetSourceKey(QMeasurementContext measurement)
        {
            string sourceName;
            string sourceId;

            if (!measurement.HasSource) return "없음";

            sourceName = measurement.Table switch
            {
                FbtHYDROMETERMPDS => "MPDS",
                FbtHYDROMETERVIDEO => "VIDEO",
                FbtWATERLEVEL => "WATERLEVEL",
                _ => string.IsNullOrWhiteSpace(measurement.TableName) ? "UNKNOWN" : measurement.TableName
            };
            sourceId = measurement.SourceId >= 0 ? $"#{measurement.SourceId}" : string.Empty;
            return $"{sourceName}{sourceId}@{measurement.SourceDate:yyyy-MM-dd} {measurement.SourceTime:HH:mm}";
        }

        public static string GetSlotKey(QCalculationContext calculation)
        {
            return calculation.SlotId >= 0
                ? $"#{calculation.SlotId}@{calculation.SlotDate:yyyy-MM-dd} {calculation.SlotTime:HH:mm}"
                : "없음";
        }
    }
}
