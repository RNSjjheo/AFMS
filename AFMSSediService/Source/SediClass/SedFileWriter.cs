using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AFMSSediService
{
    internal sealed class SedFileWriter
    {
        private const string Header =
            "St_code,YYYYMMDDhhmm,Deci_All,Deci_VTH,AC,DC_Charge,DC_Battery," +
            "Temp_Sys,Hr_Sys,Deci_WL,WaterDepth,WaterLevel,WL_Offset,Salinity," +
            "No_ADVM,ADVMType,SSC,Sedment,TotalSed,StartCell,EndSell,Dec_ADVM," +
            "Temp_Water,Depth_ADVM,Pitch,Roll,WN,WS,WP,WF,DIS1,DIS2," +
            "No_Cell,V_EW,V_NS,E1,E2";

        private readonly string dataDirectory;

        public SedFileWriter(string configuredDirectory)
        {
            dataDirectory = Path.IsPathRooted(configuredDirectory)
                ? configuredDirectory
                : Path.Combine(AppContext.BaseDirectory, configuredDirectory);
            Directory.CreateDirectory(dataDirectory);
        }

        public async Task<string> WriteAsync(
            SediSedimentRecord record,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(record);
            if (record.MeasurementTime.Length != 12)
                throw new InvalidOperationException("SED 측정 시각은 YYYYMMDDhhmm 형식이어야 합니다.");

            string stationDirectoryName = SanitizeFileName(record.StationCode);
            string measureDate = record.MeasurementTime[..8];
            string monthDirectory = $"{stationDirectoryName}_{measureDate[..6]}";
            string dayDirectory = $"{stationDirectoryName}_{measureDate}";
            string directory = Path.Combine(dataDirectory, monthDirectory, dayDirectory);
            Directory.CreateDirectory(directory);

            string fileName = $"{stationDirectoryName}_{record.MeasurementTime}.sed";
            string path = Path.Combine(directory, fileName);
            string temporaryPath = path + ".tmp";

            string contents = Header + "\r\n" + BuildDataLine(record) + "\r\n";
            await File.WriteAllTextAsync(
                temporaryPath,
                contents,
                new UTF8Encoding(false),
                cancellationToken);
            File.Move(temporaryPath, path, true);
            return path;
        }

        private static string BuildDataLine(SediSedimentRecord record)
        {
            List<string> fields =
            [
                record.StationCode,
                record.MeasurementTime,
                record.OverallDecision,
                record.VthDecision,
                Integer(record.Ac),
                Number(record.DcCharge),
                Number(record.DcBattery),
                Number(record.SystemTemperature),
                Number(record.SystemHumidity),
                record.WaterLevelDecision,
                Number(record.WaterDepth),
                Number(record.WaterLevel),
                Number(record.WaterLevelOffset),
                Number(record.Salinity)
            ];

            foreach (SediAdvmRecord advm in record.Advms.OrderBy(item => item.Number))
            {
                fields.AddRange(
                [
                    Integer(advm.Number),
                    Integer(advm.Type),
                    Number(advm.Ssc),
                    Number(advm.Sediment),
                    Number(advm.TotalSediment),
                    Integer(advm.StartCell),
                    Integer(advm.EndCell),
                    advm.Decision,
                    Number(advm.WaterTemperature),
                    Number(advm.Depth),
                    Number(advm.Pitch),
                    Number(advm.Roll),
                    Integer(advm.CellCount),
                    Integer(advm.CellSize),
                    Integer(advm.PingCount),
                    Integer(advm.Frequency),
                    Integer(advm.FirstCellDistance),
                    Integer(advm.LastCellDistance)
                ]);

                foreach (SediCellRecord cell in advm.Cells.OrderBy(item => item.Number))
                {
                    fields.AddRange(
                    [
                        Integer(cell.Number),
                        Integer(cell.VelocityEastWest),
                        Integer(cell.VelocityNorthSouth),
                        Integer(cell.Echo1),
                        Integer(cell.Echo2)
                    ]);
                }
            }

            return string.Join(',', fields.Select(EscapeCsv));
        }

        private static string Integer(int value) =>
            value.ToString(CultureInfo.InvariantCulture);

        private static string Number(double value) =>
            value.ToString("0.##########", CultureInfo.InvariantCulture);

        private static string EscapeCsv(string value)
        {
            if (value.IndexOfAny([',', '"', '\r', '\n']) < 0) return value;
            return '"' + value.Replace("\"", "\"\"") + '"';
        }

        private static string SanitizeFileName(string value)
        {
            HashSet<char> invalid = Path.GetInvalidFileNameChars().ToHashSet();
            string result = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return string.IsNullOrWhiteSpace(result) ? "UNKNOWN" : result;
        }
    }
}
