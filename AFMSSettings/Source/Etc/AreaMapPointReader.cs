using AFMSDll;
using System;
using System.Globalization;
using System.Text;

namespace AFMSSettings
{
    public static class AreaMapPointReader
    {
        static AreaMapPointReader()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static AreaPointDatas Read(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("파일 경로가 비어 있습니다.", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("단면 CSV 파일을 찾을 수 없습니다.", filePath);

            if (!string.Equals(Path.GetExtension(filePath), ".csv", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("CSV 파일만 읽을 수 있습니다.");
            }

            string text = ReadText(filePath);
            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            if (lines.Length <= 1) throw new InvalidDataException("유효한 단면 좌표가 없습니다.");

            AreaPointDatas points = new();

            // 첫 번째 행은 헤더 여부와 관계없이 무조건 무시합니다.
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] values = line.Split(',');

                // 앞의 두 열만 사용하며, 세 번째 열부터는 모두 무시합니다.
                if (values.Length < 2) throw new InvalidDataException($"{i + 1}행에 필요한 두 개의 열이 없습니다.");

                string distanceText = values[0].Trim().Trim('\uFEFF');
                string elevationText = values[1].Trim();

                // 첫 두 열이 모두 비어 있는 행은 빈 행으로 간주하고 무시합니다.
                if (distanceText.Length == 0 && elevationText.Length == 0) continue;

                if (distanceText.Length == 0 || elevationText.Length == 0)
                {
                    throw new InvalidDataException($"{i + 1}행의 거리 또는 높이 값이 비어 있습니다.");
                }

                if (!double.TryParse(distanceText, NumberStyles.Float, CultureInfo.InvariantCulture, out double distance))
                {
                    throw new InvalidDataException($"{i + 1}행의 첫 번째 열 값이 숫자가 아닙니다. 값: {distanceText}");
                }

                if (!double.TryParse(elevationText, NumberStyles.Float, CultureInfo.InvariantCulture, out double elevation))
                {
                    throw new InvalidDataException($"{i + 1}행의 두 번째 열 값이 숫자가 아닙니다. 값: {elevationText}");
                }

                points.Add(new AreaPoint(points.Count, distance, elevation));
            }

            if (points.Count == 0) throw new InvalidDataException("유효한 단면 좌표가 없습니다.");

            return points;
        }

        public static bool TryRead(string filePath, out AreaPointDatas points, out string errorMessage)
        {
            try
            {
                points = Read(filePath);
                errorMessage = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                points = new AreaPointDatas();
                errorMessage = ex.Message;
                return false;
            }
        }

        private static string ReadText(string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);

            try
            {
                UTF8Encoding utf8 = new UTF8Encoding(false, true);
                return utf8.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                Encoding cp949 = Encoding.GetEncoding(949);
                return cp949.GetString(bytes);
            }
        }
    }
}
