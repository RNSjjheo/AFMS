using System.Globalization;
using System.Text;

namespace AFMSSediService
{
    internal static class SscCalculationLogFormatter
    {
        public static string Format(
            SscCalculationSlot slot,
            ChannelMasterSource source,
            ChannelMasterMeasurement measurement,
            SSCDeviceProfile profile,
            SscCalculationResult result)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(measurement);
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentNullException.ThrowIfNull(result);

            double cellSize = measurement.CellSizeCm / 100.0;
            double blankDistance = measurement.Frequency / 100.0;
            double beamAngleRadians = profile.BeamAngle * Math.PI / 180.0;
            double beamCosine = Math.Cos(beamAngleRadians);
            double exponent = profile.SscA * result.AverageScb + profile.SscB;
            StringBuilder text = new();

            text.AppendLine("[SSC CALCULATION DETAIL]");
            text.AppendLine($"SlotId={slot.Id}, SlotTime={slot.SlotTime:yyyy-MM-dd HH:mm:ss}");
            text.AppendLine($"Source={source.HeaderTable}/{source.CellTable}@{measurement.Key.MeasureDate} {measurement.Key.MeasureTime}");
            text.AppendLine($"Profile: Device={profile.DeviceType}, HydroTable={profile.HydroTableName}, Cells={profile.CellFrom}~{profile.CellTo}");
            text.AppendLine($"Profile: KValue={Number(profile.KValue)}, BeamAngle={Number(profile.BeamAngle)} deg, SSCA={Number(profile.SscA)}, SSCB={Number(profile.SscB)}");
            text.AppendLine($"Measurement: Temperature={Number(measurement.Temperature)} C, Depth={Number(measurement.Depth)} m, CellCount={measurement.CellCount}");
            text.AppendLine($"Measurement: CellSize={measurement.CellSizeCm} cm ({Number(cellSize)} m), BlankDistance={measurement.Frequency} cm ({Number(blankDistance)} m)");
            text.AppendLine($"Measurement: Pitch={Number(measurement.Pitch)} deg, Roll={Number(measurement.Roll)} deg, PingCount={measurement.PingCount}");
            text.AppendLine($"Geometry: BeamAngleRadians={Number(beamAngleRadians)}, cos(BeamAngle)={Number(beamCosine)}");
            text.AppendLine("Cell calculation: Mb=KValue*((Echo1+Echo2)/2), Range=(BlankDistance+Index*CellSize+CellSize/2)/cos(BeamAngle)");
            text.AppendLine("Cell calculation: WCB=Mb+SpreadingLoss+WaterAbsorptionLoss, SCB=WCB+2*Range*SedimentAttenuation");
            text.AppendLine("Cell | Echo1 | Echo2 | AvgEcho | Mb | Range(m) | SpreadCoeff | SpreadLoss | WaterAbs | WaterAbsLoss | SedimentAtt | WCB | SCB");

            foreach (SscCellCalculation cell in result.Cells)
            {
                text.AppendLine($"{cell.CellNumber} | {cell.Echo1} | {cell.Echo2} | {Number(cell.AverageEcho)} | {Number(cell.Mb)} | {Number(cell.Range)} | " +
                    $"{Number(cell.SpreadingCoefficient)} | {Number(cell.SpreadingLoss)} | {Number(cell.WaterAbsorption)} | " +
                    $"{Number(cell.WaterAbsorptionLoss)} | {Number(cell.SedimentAttenuation)} | {Number(cell.WaterCorrectedBackscatter)} | " +
                    Number(cell.SedimentCorrectedBackscatter));
            }

            text.AppendLine($"Regression: Count={result.Cells.Count}, Slope=SUM((Range-MeanRange)*(WCB-MeanWCB))/SUM((Range-MeanRange)^2)={Number(result.RegressionSlope)}");
            text.AppendLine($"Regression: Intercept=MeanWCB-MeanRange*Slope={Number(result.RegressionIntercept)}");
            text.AppendLine($"SedimentAttenuation=-0.5*Slope={Number(-0.5 * result.RegressionSlope)}");
            text.AppendLine($"AverageSCB=AVERAGE(Cell SCB)={Number(result.AverageScb)}");
            text.AppendLine($"Exponent=SSCA*AverageSCB+SSCB={Number(profile.SscA)}*{Number(result.AverageScb)}+{Number(profile.SscB)}={Number(exponent)}");
            text.AppendLine($"SSC=10^Exponent=10^{Number(exponent)}={Number(result.Ssc)} mg/L");
            text.AppendLine($"TotalSand=SSC*Discharge*0.0864={Number(result.Ssc)}*{Number(result.Discharge1)}*0.0864={Number(result.TotalSand1)} ton/day");
            return text.ToString().TrimEnd();
        }

        private static string Number(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
    }
}
