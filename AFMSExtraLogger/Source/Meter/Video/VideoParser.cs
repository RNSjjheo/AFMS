using AFMSDll;
using log4net;
using Newtonsoft.Json.Linq;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AFMSExtraLogger
{
    public class VideoParser
    {
        public static MeasureVideo? Converting(string msg, out string errorMsg)
        {
            MeasureVideo Data = new MeasureVideo();
            JObject json;

            errorMsg = string.Empty;

            try
            {
                json = JObject.Parse(msg);
            }
            catch (Exception ex)
            {
                errorMsg = $"Failed to parse JSON: {ex.Message}";
                return null;
            }

            Data.SiteCode = DiagnosticsOwner.Instance.SiteCode;
            Data.DeviceType = ReadEnum(json, MeasureVideo.KEY_DEVICE_TYPE, HydroVideoType.NONE, out errorMsg);
            if (errorMsg != string.Empty) return null;

            Data.Datetime = ReadDatetime(json, MeasureVideo.KEY_DATE_TIME, DateTime.Now, out errorMsg);
            if (errorMsg != string.Empty) return null;

            Data.Status = ReadEnum(json, MeasureVideo.KEY_STATUS, VideoMeasureStatus.Error, out errorMsg);
            if (errorMsg != string.Empty) return null;

            Data.Interval = ReadInt(json, MeasureVideo.KEY_INTERVAL, 0, out errorMsg);
            if (errorMsg != string.Empty) return null;

            Data.WaterLevel = ReadDouble(json, MeasureVideo.KEY_WATER_LEVEL, 0.0, out errorMsg);
            if (errorMsg != string.Empty) return null;

            Data.Area = ReadDouble(json, MeasureVideo.KEY_AREA, 0.0, out errorMsg);
            if (errorMsg != string.Empty) return null;

            Data.AreaUncertainty = ReadDouble(json, MeasureVideo.KEY_AREA_UNCERATAINLY, 0.0, out errorMsg);
            if (errorMsg != string.Empty) return null;

            Data.Velocity = ReadDouble(json, MeasureVideo.KEY_VELOCITY, 0.0, out errorMsg);
            if (errorMsg != string.Empty) return null;

            Data.VeloUncertainty = ReadDouble(json, MeasureVideo.KEY_VELO_UNCERATAINLY, 0.0, out errorMsg);
            if (errorMsg != string.Empty) return null;

            Data.Disc = ReadDouble(json, MeasureVideo.KEY_DISCHARGE, 0.0, out errorMsg);
            if (errorMsg != string.Empty) return null;

            Data.DiscUncertainty = ReadDouble(json, MeasureVideo.KEY_DISC_UNCERATAINLY, 0.0, out errorMsg);
            if (errorMsg != string.Empty) return null;

            Data.CellCount = ReadInt(json, MeasureVideo.KEY_CELL_COUNT, 0, out errorMsg);
            if (errorMsg != string.Empty) return null;

            Data.CellLength = ReadDouble(json, MeasureVideo.KEY_CELL_LENGTH, 0.0, out errorMsg);
            if (errorMsg != string.Empty) return null;

            for (int i = 0; i < Data.CellCount; i++)
            {
                int no = i + 1;
                string keyN = $"N{no}";
                string keyV = $"V{no}";
                string keyX = $"X{no}";
                string keyY = $"Y{no}";
                string keyU = $"U{no}";

                MeasureVideoCell cell = new MeasureVideoCell();
                cell.No = ReadInt(json, keyN, 0, out errorMsg);
                if (errorMsg != string.Empty) return null;

                cell.Velocity = ReadDouble(json, keyV, 0.0, out errorMsg);
                if (errorMsg != string.Empty) return null;

                cell.PosX = ReadDouble(json, keyX, 0.0, out errorMsg);
                if (errorMsg != string.Empty) return null;

                cell.PosY = ReadDouble(json, keyY, 0.0, out errorMsg);
                if (errorMsg != string.Empty) return null;

                cell.Uncertainty = ReadDouble(json, keyU, 0.0, out errorMsg);
                if (errorMsg != string.Empty) return null;

                Data.Cells.Add(cell);
            }

            return Data;
        }

        private static DateTime ReadDatetime(JObject json, string key, DateTime defTime, out string errorMsg)
        {
            errorMsg = string.Empty;

            if (!json.ContainsKey(key))
            {
                PrintNotFound(key);
                return defTime;
            }

            string dateTimeText = ReadString(json, MeasureVideo.KEY_DATE_TIME, "", out errorMsg);

            if (!DateTime.TryParseExact(dateTimeText, "yyyyMMddHHmm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDatetime))
            {
                PrintTypeError(MeasureVideo.KEY_DATE_TIME);
                return defTime;
            }

            return parsedDatetime;
        }

        private static string ReadString(JObject json, string key, string defaultValue, out string errorMsg)
        {
            errorMsg = string.Empty;

            if (!json.ContainsKey(key))
            {
                errorMsg = PrintNotFound(key);
                return defaultValue;
            }

            JToken? token = json[key];

            if (token == null || token.Type == JTokenType.Null)
            {
                errorMsg = PrintTypeError(key);
                return defaultValue;
            }

            return token.ToString();
        }

        private static int ReadInt(JObject json, string key, int defaultValue, out string errorMsg)
        {
            errorMsg = string.Empty;

            if (!json.ContainsKey(key))
            {
                errorMsg = PrintNotFound(key);
                return defaultValue;
            }   

            string value = json[key]?.ToString() ?? "";

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            {
                return result;
            }

            errorMsg = PrintTypeError(key);
            return defaultValue;
        }

        private static double ReadDouble(JObject json, string key, double defaultValue, out string errorMsg)
        {
            errorMsg = string.Empty;

            if (!json.ContainsKey(key))
            {
                errorMsg = PrintNotFound(key);
                return defaultValue;
            }

            string value = json[key]?.ToString() ?? "";

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            errorMsg = PrintTypeError(key);
            return defaultValue;
        }

        private static TEnum ReadEnum<TEnum>(JObject json, string key, TEnum defaultValue, out string errorMsg)
            where TEnum : struct, Enum
        {
            errorMsg = string.Empty;

            JToken? token = json[key];

            if (token == null || token.Type == JTokenType.Null)
            { 
                errorMsg = $"{key} is null or undefined";
                return defaultValue;
            }

            string? value = token.ToString();

            if (string.IsNullOrWhiteSpace(value))
            {
                errorMsg = $"{key} is empty";
                return defaultValue;
            }

            if (!Enum.TryParse(value, ignoreCase: true, out TEnum result))
            {
                errorMsg = $"{key} has an invalid value";
                return defaultValue;
            }

            if (!Enum.IsDefined(typeof(TEnum), result))
            {
                errorMsg = $"{key} has an invalid value";
                return defaultValue;
            }

            return result;
        }

        private static string PrintTypeError(string key)
        {
            return $"{key} is type error";
        }

        private static string PrintNotFound(string key)
        {
            return $"{key} is not found";
        }
    }

}
