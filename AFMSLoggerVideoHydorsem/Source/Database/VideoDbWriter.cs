using System.Globalization;
using AFMSDll;

namespace AFMSLoggerVideoHydorsem
{
    public static class VideoDbWriter
    {
        public static bool Insert(MeasureVideo data)
        {
            data.Id = FBProvider.Instance.GetNextID(FbtHYDROMETERVIDEO.TABLE_NAME);
            if (data.Id == 0 || !InsertMain(data)) return false;

            foreach (MeasureVideoCell cell in data.Cells)
            {
                cell.VideoId = data.Id;
                cell.Id = FBProvider.Instance.GetNextID(FbtHYDROMETERVIDEOCELL.TABLE_NAME);
                if (cell.Id == 0 || !InsertDetail(cell)) return false;
            }

            return true;
        }

        private static bool InsertMain(MeasureVideo data)
        {
            string sql = $"INSERT INTO {FbtHYDROMETERVIDEO.TABLE_NAME}(";
            sql += $"\n{FbtHYDROMETERVIDEO.COL_ID}";
            sql += $",\n{FbtHYDROMETERVIDEO.COL_MEASURE_DATE}";
            sql += $",\n{FbtHYDROMETERVIDEO.COL_MEASURE_TIME}";
            sql += $",\n{FbtHYDROMETERVIDEO.COL_SITE_CODE}";
            sql += $",\n{FbtHYDROMETERVIDEO.COL_DEVICE_TYPE}";
            sql += $",\n{FbtHYDROMETERVIDEO.COL_STATUS}";
            sql += $",\n{FbtHYDROMETERVIDEO.COL_INTERVAL}";
            sql += $",\n{FbtHYDROMETERVIDEO.COL_WATERLEVEL}";
            sql += $",\n{FbtHYDROMETERVIDEO.COL_AREA}";
            sql += $",\n{FbtHYDROMETERVIDEO.COL_AREA_UNCERTAINTY}";
            sql += $",\n{FbtHYDROMETERVIDEO.COL_VELO}";
            sql += $",\n{FbtHYDROMETERVIDEO.COL_VELO_UNCERTAINTY}";
            sql += $",\n{FbtHYDROMETERVIDEO.COL_DISC}";
            sql += $",\n{FbtHYDROMETERVIDEO.COL_DISC_UNCERTAINTY}";
            sql += $",\n{FbtHYDROMETERVIDEO.COL_CELL_COUNT}";
            sql += $",\n{FbtHYDROMETERVIDEO.COL_CELL_LENGTH}";
            sql += "\n) VALUES (";
            sql += $"\n{data.Id}";
            sql += $",\n'{data.Datetime:yyyyMMdd}'";
            sql += $",\n'{data.Datetime:HHmmss}'";
            sql += $",\n'{data.SiteCode}'";
            sql += $",\n{(int)data.DeviceType}";
            sql += $",\n{(int)data.Status}";
            sql += $",\n{data.Interval}";
            sql += $",\n{Format(data.WaterLevel)}";
            sql += $",\n{Format(data.Area)}";
            sql += $",\n{Format(data.AreaUncertainty)}";
            sql += $",\n{Format(data.Velocity)}";
            sql += $",\n{Format(data.VeloUncertainty)}";
            sql += $",\n{Format(data.Disc)}";
            sql += $",\n{Format(data.DiscUncertainty)}";
            sql += $",\n{data.CellCount}";
            sql += $",\n{Format(data.CellLength)}";
            sql += "\n)";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.RunNonQuery(sql);
            return true;
        }

        private static bool InsertDetail(MeasureVideoCell data)
        {
            string sql = $"INSERT INTO {FbtHYDROMETERVIDEOCELL.TABLE_NAME}(";
            sql += $"\n{FbtHYDROMETERVIDEOCELL.COL_ID}";
            sql += $",\n{FbtHYDROMETERVIDEOCELL.COL_VIDEO_ID}";
            sql += $",\n{FbtHYDROMETERVIDEOCELL.COL_CELL_NO}";
            sql += $",\n{FbtHYDROMETERVIDEOCELL.COL_VELOCITY}";
            sql += $",\n{FbtHYDROMETERVIDEOCELL.COL_POS_X}";
            sql += $",\n{FbtHYDROMETERVIDEOCELL.COL_POS_Y}";
            sql += $",\n{FbtHYDROMETERVIDEOCELL.COL_UNCERTAINTY}";
            sql += "\n) VALUES (";
            sql += $"\n{data.Id}";
            sql += $",\n{data.VideoId}";
            sql += $",\n{data.No}";
            sql += $",\n{Format(data.Velocity)}";
            sql += $",\n{Format(data.PosX)}";
            sql += $",\n{Format(data.PosY)}";
            sql += $",\n{Format(data.Uncertainty)}";
            sql += "\n)";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.RunNonQuery(sql);
            return true;
        }

        private static string Format(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
