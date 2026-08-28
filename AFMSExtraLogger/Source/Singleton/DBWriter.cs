using AFMSDll;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AFMSExtraLogger
{
    public static class DBWriter
    {
        public static bool  VideoInsert(MeasureVideo data)
        {
            data.Id = FBProvider.Instance.GetNextID(FbtHYDROMETERVIDEO.TABLE_NAME);
            if (data.Id == 0) return false;

            bool main = InsertMain(data);
            if (!main) return false;

            foreach (MeasureVideoCell cell in data.Cells)
            {
                cell.VideoId = data.Id;
                cell.Id = FBProvider.Instance.GetNextID(FbtHYDROMETERVIDEOCELL.TABLE_NAME);
                if (cell.Id == 0) return false;

                bool detail = InsertDetail(cell);
                if (!detail) return false;
            }

            return true;
        }

        public static bool InsertMPDS(MeasurementBatch data)
        {
            if(!IsNewMPDSData(data))
            {
                TcpBrocastBuffer.WriteLog("_RF", $"DB에 이미 기록된 데이터입니다.({data.Info.MeasureKey})");
                return true;
            }

            data.Id = FBProvider.Instance.GetNextID(FbtHYDROMETERMPDS.TABLE_NAME);
            if (data.Id == 0) return false;

            bool main = InsertMPDSMain(data);
            if (!main) return false;

            foreach (MPDSCell cell in data.Cells)
            {
                cell.MpdsId = data.Id;
                cell.Id = FBProvider.Instance.GetNextID(FbtHYDROMETERMPDSCELL.TABLE_NAME);
                if (cell.Id == 0) return false;

                bool detail = InsertMPDSDetail(cell);
                if (!detail) return false;
            }

            return true;
        }

        private static bool InsertMain(MeasureVideo data)
        {
            string sql = $"INSERT INTO {FbtHYDROMETERVIDEO.TABLE_NAME}(";
            sql += "\n" + $"{FbtHYDROMETERVIDEO.COL_ID}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEO.COL_MEASURE_DATE}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEO.COL_MEASURE_TIME}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEO.COL_SITE_CODE}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEO.COL_DEVICE_TYPE}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEO.COL_STATUS}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEO.COL_INTERVAL}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEO.COL_WATERLEVEL}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEO.COL_AREA}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEO.COL_AREA_UNCERTAINTY}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEO.COL_VELO}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEO.COL_VELO_UNCERTAINTY}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEO.COL_DISC}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEO.COL_DISC_UNCERTAINTY}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEO.COL_CELL_COUNT}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEO.COL_CELL_LENGTH}";
            sql += "\n" + $")VALUES (";
            sql += "\n" + $"{data.Id}";
            sql += ",\n" + $"'{data.Datetime.ToString("yyyyMMdd")}'";
            sql += ",\n" + $"'{data.Datetime.ToString("HHmmss")}'";
            sql += ",\n" + $"'{data.SiteCode}'";
            sql += ",\n" + $"{(int)data.DeviceType}";
            sql += ",\n" + $"{(int)data.Status}";
            sql += ",\n" + $"{data.Interval}";
            sql += ",\n" + $"{data.WaterLevel.ToString("0.00")}";
            sql += ",\n" + $"{data.Area.ToString("0.00")}";
            sql += ",\n" + $"{data.AreaUncertainty.ToString("0.00")}";
            sql += ",\n" + $"{data.Velocity.ToString("0.000")}";
            sql += ",\n" + $"{data.VeloUncertainty.ToString("0.000")}";
            sql += ",\n" + $"{data.Disc.ToString("0.00")}";
            sql += ",\n" + $"{data.DiscUncertainty.ToString("0.00")}";
            sql += ",\n" + $"{data.CellCount}";
            sql += ",\n" + $"{data.CellLength}";
            sql += "\n" + $")";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.RunNonQuery(sql);

            return true;
        }

        private static bool InsertMPDSMain(MeasurementBatch data)
        {
            string sql = $"INSERT INTO {FbtHYDROMETERMPDS.TABLE_NAME}(";
            sql += "\n" + $"{FbtHYDROMETERMPDS.COL_ID}";
            sql += ",\n" + $"{FbtHYDROMETERMPDS.COL_MEASURE_DATE}";
            sql += ",\n" + $"{FbtHYDROMETERMPDS.COL_MEASURE_TIME}";
            sql += ",\n" + $"{FbtHYDROMETERMPDS.COL_POINT_CODE}";
            sql += ",\n" + $"{FbtHYDROMETERMPDS.COL_DEVICE_COUNT}";
            sql += ",\n" + $"{FbtHYDROMETERMPDS.COL_DEVICE_VOLT}";
            sql += ",\n" + $"{FbtHYDROMETERMPDS.COL_WATER_LEVEL}";
            sql += ",\n" + $"{FbtHYDROMETERMPDS.COL_WIND_SPEED}";
            sql += ",\n" + $"{FbtHYDROMETERMPDS.COL_WIND_GUST}";
            sql += ",\n" + $"{FbtHYDROMETERMPDS.COL_WIND_DIRECTION}";
            sql += ",\n" + $"{FbtHYDROMETERMPDS.COL_TEMPERATURE}";
            sql += ",\n" + $"{FbtHYDROMETERMPDS.COL_HUMIDITY}";
            sql += ",\n" + $"{FbtHYDROMETERMPDS.COL_ATMOSPHERE}";
            sql += ",\n" + $"{FbtHYDROMETERMPDS.COL_COLLACTOR_RSSI}";
            sql += ",\n" + $"{FbtHYDROMETERMPDS.COL_RESERVED1}";
            sql += "\n" + $")VALUES (";
            sql += "\n" + $"{data.Id}";
            sql += ",\n" + $"'{data.Info.MeasureTime.ToString("yyyyMMdd")}'";
            sql += ",\n" + $"'{data.Info.MeasureTime.ToString("HHmmss")}'";
            sql += ",\n" + $"'{data.Info.PointCode}'";
            sql += ",\n" + $"{(int)data.Info.DeviceCount}";
            sql += ",\n" + $"{data.Info.CollectorVolt.ToString("0.00")}";
            sql += ",\n" + $"{data.Info.Waterlevel.ToString("0.00")}";
            sql += ",\n" + $"{data.Wind.WindSpeed.ToString("0.000")}";
            sql += ",\n" + $"{data.Wind.WindGust.ToString("0.00")}";
            sql += ",\n" + $"{data.Wind.WindDirection.ToString("0.00")}";
            sql += ",\n" + $"{data.Wind.Temperature.ToString("0.00")}";
            sql += ",\n" + $"{data.Wind.Humidity.ToString("0.00")}";
            sql += ",\n" + $"{data.Wind.Atmosphere.ToString("0.00")}";
            sql += ",\n" + $"{data.CollectorRSSI.ToString()}";
            sql += ",\n" + $"0";
            sql += "\n" + $")";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.RunNonQuery(sql);

            return true;
        }

        private static bool InsertDetail(MeasureVideoCell data)
        {
            string sql = $"INSERT INTO {FbtHYDROMETERVIDEOCELL.TABLE_NAME}(";
            sql += "\n" + $"{FbtHYDROMETERVIDEOCELL.COL_ID}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEOCELL.COL_VIDEO_ID}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEOCELL.COL_CELL_NO}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEOCELL.COL_VELOCITY}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEOCELL.COL_POS_X}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEOCELL.COL_POS_Y}";
            sql += ",\n" + $"{FbtHYDROMETERVIDEOCELL.COL_UNCERTAINTY}";
            sql += "\n" + $")VALUES (";
            sql += "\n" + $"{data.Id}";
            sql += ",\n" + $"{data.VideoId}";
            sql += ",\n" + $"{data.No}";
            sql += ",\n" + $"{data.Velocity}";
            sql += ",\n" + $"{data.PosX}";
            sql += ",\n" + $"{data.PosY}";
            sql += ",\n" + $"{data.Uncertainty}";
            sql += "\n" + $")";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.RunNonQuery(sql);

            return true;
        }

        private static bool InsertMPDSDetail(MPDSCell data)
        {
            string sql = $"INSERT INTO {FbtHYDROMETERMPDSCELL.TABLE_NAME}(";
            sql += "\n" + $"{FbtHYDROMETERMPDSCELL.COL_ID}";            //1
            sql += ",\n" + $"{FbtHYDROMETERMPDSCELL.COL_MPDS_ID}";      //2
            sql += ",\n" + $"{FbtHYDROMETERMPDSCELL.COL_DEV_NO}";
            sql += ",\n" + $"{FbtHYDROMETERMPDSCELL.COL_DEV_STATUS}";   //4
            sql += ",\n" + $"{FbtHYDROMETERMPDSCELL.COL_DEV_TYPE}";
            sql += ",\n" + $"{FbtHYDROMETERMPDSCELL.COL_BOARD_VOLT}";   //6
            sql += ",\n" + $"{FbtHYDROMETERMPDSCELL.COL_SNR}";
            sql += ",\n" + $"{FbtHYDROMETERMPDSCELL.COL_WATER_LEVEL}";
            sql += ",\n" + $"{FbtHYDROMETERMPDSCELL.COL_VELOCITY}";     //9
            sql += ",\n" + $"{FbtHYDROMETERMPDSCELL.COL_DISCHARGE}";
            sql += ",\n" + $"{FbtHYDROMETERMPDSCELL.COL_FVELOCITY}";    //11
            sql += ",\n" + $"{FbtHYDROMETERMPDSCELL.COL_FDISCHARGE}";
            sql += ",\n" + $"{FbtHYDROMETERMPDSCELL.COL_OPPOSITE}";     //13
            sql += ",\n" + $"{FbtHYDROMETERMPDSCELL.COL_INCLINATION}";
            sql += ",\n" + $"{FbtHYDROMETERMPDSCELL.COL_RFRSSI}";       //15
            sql += ",\n" + $"{FbtHYDROMETERMPDSCELL.COL_VSTDUNCERT}";
            sql += ",\n" + $"{FbtHYDROMETERMPDSCELL.COL_VEXTUNCERT}";
            sql += "\n" + $")VALUES (";
            sql += "\n" + $"{data.Id}";                 //1
            sql += ",\n" + $"{data.MpdsId}";            //2
            sql += ",\n" + $"{(int)data.DeviceNumber}";
            sql += ",\n" + $"{data.DeviceStatus}";      //4
            sql += ",\n" + $"'{data.DeviceType.ToString()}'";
            sql += ",\n" + $"{data.BoardVolt}";         //6
            sql += ",\n" + $"{data.Snr}";
            sql += ",\n" + $"{data.WaterLevel.ToString("0.00")}";
            sql += ",\n" + $"{data.Velocity.ToString("0.000")}";    //9
            sql += ",\n" + $"{data.Discharge.ToString("0.000")}";
            sql += ",\n" + $"{data.FilterVelocity.ToString("0.000")}";  //11
            sql += ",\n" + $"{data.FilterDischarge.ToString("0.000")}";
            sql += ",\n" + $"{data.Opposite.ToString("0.000")}";        //13
            sql += ",\n" + $"{data.Inclination.ToString("0.00")}";
            sql += ",\n" + $"{data.RfRssi.ToString()}";                 //15
            sql += ",\n" + $"{data.VelocityStandardUncertainty.ToString("0.00")}";
            sql += ",\n" + $"{data.VelocityExpandedUncertainty.ToString("0.00")}";
            sql += "\n" + $")";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.RunNonQuery(sql);

            return true;
        }

        private static bool IsNewMPDSData(MeasurementBatch data)
        {
            string sql = $"SELECT COUNT(*) AS DATA_COUNT";
            sql += "\n" + $"FROM {FbtHYDROMETERMPDS.TABLE_NAME}";
            sql += "\n" + $"WHERE {FbtHYDROMETERMPDS.COL_MEASURE_DATE} = '{data.Info.MeasureTime.ToString("yyyyMMdd")}'";
            sql += "\n" + $"AND {FbtHYDROMETERMPDS.COL_MEASURE_TIME} = '{data.Info.MeasureTime.ToString("HHmmss")}'";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.RunQuery(sql);

            foreach (DataRow row in db.Results.Rows)
            {
                return row[0].ToString() == "0";
            }

            return false;
        }
    }
}
