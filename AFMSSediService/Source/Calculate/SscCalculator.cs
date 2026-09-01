using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSSediService
{
    internal static class SscCalculator
    {
        public static SscCalculationResult Calculate(
            ChannelMasterMeasurement source,
            RSandDeviceProfile profile,
            double discharge)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(profile);

            if (!profile.IsEnabled)
                throw new InvalidOperationException("비활성 SSC 프로파일은 계산할 수 없습니다.");

            if (source.Cells.Count == 0)
                throw new InvalidOperationException("ChannelMaster 셀 데이터가 없습니다.");

            double cellSize = source.CellSizeCm / 100.0;
            double blankDistance = source.Frequency / 100.0;
            if (cellSize <= 0.0)
                throw new InvalidOperationException("ChannelMaster 셀 크기가 0 이하입니다.");

            double beamAngleRadians = profile.BeamAngle * Math.PI / 180.0;
            double beamCosine = Math.Cos(beamAngleRadians);
            if (Math.Abs(beamCosine) < 1e-12)
                throw new InvalidOperationException("빔 각도로 셀 거리를 계산할 수 없습니다.");

            List<WorkingCell> working = [];
            for (int index = 0; index < source.Cells.Count; index++)
            {
                ChannelMasterCell cell = source.Cells[index];
                int cellNumber = index + 1;
                if (cellNumber < profile.CellFrom || cellNumber > profile.CellTo) continue;

                double mb = profile.KValue * ((cell.Echo1 + cell.Echo2) / 2.0);
                double range = (blankDistance + index * cellSize + cellSize / 2.0) / beamCosine;
                working.Add(new WorkingCell(cell.Number, mb, range));
            }

            if (working.Count < 2)
                throw new InvalidOperationException("SSC 회귀 계산에는 유효 셀이 2개 이상 필요합니다.");

            int frequencyKhz = GetFrequencyKhz(profile.DeviceType);
            double rayleighDistance = GetRayleighDistance(profile.DeviceType);

            foreach (WorkingCell cell in working)
            {
                double spreadingCoefficient = 1.0;
                if (cell.Range < blankDistance && rayleighDistance > 0.0)
                {
                    double z = cell.Range / rayleighDistance;
                    spreadingCoefficient = 1.0 + 1.0 / (1.35 * z + Math.Pow(2.5 * z, 3.2));
                }

                double spreadingLoss = 20.0 * Math.Log10(spreadingCoefficient * cell.Range);
                double waterAbsorption = 8.69 *
                    ((3.38e-6 * Math.Pow(frequencyKhz, 2.0)) /
                     (21.9 * Math.Pow(10.0, 6.0 - 1520.0 / (source.Temperature + 273.0))));
                double waterAbsorptionLoss = 2.0 * waterAbsorption * cell.Range;

                cell.SpreadingCoefficient = spreadingCoefficient;
                cell.WaterAbsorption = waterAbsorption;
                cell.WaterCorrectedBackscatter = cell.Mb + spreadingLoss + waterAbsorptionLoss;
            }

            double meanRange = working.Average(cell => cell.Range);
            double meanWcb = working.Average(cell => cell.WaterCorrectedBackscatter);
            double xx = working.Sum(cell => Math.Pow(cell.Range - meanRange, 2.0));
            if (Math.Abs(xx) < 1e-12)
                throw new InvalidOperationException("SSC 회귀 계산의 셀 거리 분산이 0입니다.");

            double xy = working.Sum(cell =>
                (cell.Range - meanRange) * (cell.WaterCorrectedBackscatter - meanWcb));
            double regressionSlope = xy / xx;
            double regressionIntercept = meanWcb - meanRange * regressionSlope;
            double sedimentAttenuation = -0.5 * regressionSlope;

            foreach (WorkingCell cell in working)
            {
                cell.SedimentAttenuation = sedimentAttenuation;
                cell.SedimentCorrectedBackscatter =
                    cell.WaterCorrectedBackscatter + 2.0 * cell.Range * sedimentAttenuation;
            }

            double averageScb = working.Average(cell => cell.SedimentCorrectedBackscatter);
            double ssc = Math.Pow(10.0, profile.SscA * averageScb + profile.SscB);
            if (!double.IsFinite(ssc))
                throw new InvalidOperationException("계산된 SSC 값이 유효하지 않습니다.");

            double totalSand1 = ssc * discharge * 0.0864;
            List<SscCellCalculation> cells = working.Select(cell => new SscCellCalculation(
                cell.CellNumber,
                cell.Mb,
                cell.Range,
                cell.SpreadingCoefficient,
                cell.WaterAbsorption,
                cell.SedimentAttenuation,
                cell.WaterCorrectedBackscatter,
                cell.SedimentCorrectedBackscatter)).ToList();

            return new SscCalculationResult(
                profile.DeviceType,
                averageScb,
                regressionSlope,
                regressionIntercept,
                profile.SscA,
                profile.SscB,
                ssc,
                discharge,
                0.0,
                totalSand1,
                0.0,
                cells);
        }

        private static int GetFrequencyKhz(string deviceType) =>
            deviceType.ToUpperInvariant() switch
            {
                "CM300" => 300,
                "CM600" => 600,
                "CM1200" => 1200,
                _ => throw new InvalidOperationException($"지원하지 않는 SSC 장비 형식입니다: {deviceType}")
            };

        private static double GetRayleighDistance(string deviceType) =>
            deviceType.ToUpperInvariant() switch
            {
                "CM300" => 2.69,
                "CM600" => 2.96,
                "CM1200" => 1.71,
                _ => 0.0
            };

        private sealed class WorkingCell(int cellNumber, double mb, double range)
        {
            public int CellNumber { get; } = cellNumber;
            public double Mb { get; } = mb;
            public double Range { get; } = range;
            public double SpreadingCoefficient { get; set; }
            public double WaterAbsorption { get; set; }
            public double SedimentAttenuation { get; set; }
            public double WaterCorrectedBackscatter { get; set; }
            public double SedimentCorrectedBackscatter { get; set; }
        }
    }
}
