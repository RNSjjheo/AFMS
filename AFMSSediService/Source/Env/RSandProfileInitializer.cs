using AFMSDll;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace AFMSSediService
{
    internal static class RSandProfileInitializer
    {
        private const string START_DATE = "20260901";
        public static bool EnsureDefaultProfile()
        {
            using FBDatabase db = FBProvider.Instance.CreateDatabase();

            string countSql = $"SELECT COUNT(*) FROM {FbtRSANDPROFILE.TABLE_NAME}";
            string error = db.RunQuery(countSql);

            if (!string.IsNullOrEmpty(error))
            {
                throw new InvalidOperationException(
                    $"{FbtRSANDPROFILE.TABLE_NAME} 테이블의 데이터 확인에 실패했습니다.\n{error}");
            }

            if (HasProfile(db.Results)) return false;

            error = db.RunNonQuery(CreateDefaultInsertSql());

            if (!string.IsNullOrEmpty(error))
            {
                throw new InvalidOperationException(
                    $"{FbtRSANDPROFILE.TABLE_NAME} 기본값 저장에 실패했습니다.\n{error}");
            }

            return true;
        }

        private static bool HasProfile(DataTable result)
        {
            if (result.Rows.Count == 0) return false;

            return Convert.ToInt32(result.Rows[0][0]) > 0;
        }

        private static string CreateDefaultInsertSql()
        {
            string sql = $"INSERT INTO {FbtRSANDPROFILE.TABLE_NAME} (";
            sql += $"{FbtRSANDPROFILE.COL_PROFILE_ID}, ";
            sql += $"{FbtRSANDPROFILE.COL_PROFILE_DATE}, ";
            sql += $"{FbtRSANDPROFILE.COL_PROFILE_TIME}, ";
            sql += $"{FbtRSANDPROFILE.COL_PROFILE_NAME}, ";
            sql += $"{FbtRSANDPROFILE.COL_A_SETUP_FLAG}, ";
            sql += $"{FbtRSANDPROFILE.COL_A_DEVICE_TYPE}, ";
            sql += $"{FbtRSANDPROFILE.COL_A_VALID_CELL_TYPE}, ";
            sql += $"{FbtRSANDPROFILE.COL_A_CELL_FROM}, ";
            sql += $"{FbtRSANDPROFILE.COL_A_CELL_TO}, ";
            sql += $"{FbtRSANDPROFILE.COL_A_DB_FROM}, ";
            sql += $"{FbtRSANDPROFILE.COL_A_DB_TO}, ";
            sql += $"{FbtRSANDPROFILE.COL_A_REGRESSION}, ";
            sql += $"{FbtRSANDPROFILE.COL_A_K_VALUE}, ";
            sql += $"{FbtRSANDPROFILE.COL_A_BEAM_ANGLE}, ";
            sql += $"{FbtRSANDPROFILE.COL_A_SSC_A}, ";
            sql += $"{FbtRSANDPROFILE.COL_A_SSC_B}, ";
            sql += $"{FbtRSANDPROFILE.COL_B_SETUP_FLAG}, ";
            sql += $"{FbtRSANDPROFILE.COL_B_DEVICE_TYPE}, ";
            sql += $"{FbtRSANDPROFILE.COL_B_VALID_CELL_TYPE}, ";
            sql += $"{FbtRSANDPROFILE.COL_B_CELL_FROM}, ";
            sql += $"{FbtRSANDPROFILE.COL_B_CELL_TO}, ";
            sql += $"{FbtRSANDPROFILE.COL_B_DB_FROM}, ";
            sql += $"{FbtRSANDPROFILE.COL_B_DB_TO}, ";
            sql += $"{FbtRSANDPROFILE.COL_B_REGRESSION}, ";
            sql += $"{FbtRSANDPROFILE.COL_B_K_VALUE}, ";
            sql += $"{FbtRSANDPROFILE.COL_B_BEAM_ANGLE}, ";
            sql += $"{FbtRSANDPROFILE.COL_B_SSC_A}, ";
            sql += $"{FbtRSANDPROFILE.COL_B_SSC_B})";
            sql += " VALUES (";
            sql += "1, ";
            sql += $"'{START_DATE}', ";
            sql += "'000000', ";
            sql += $"'{START_DATE}_000000', ";
            sql += "'Y', ";
            sql += "'CM600', ";
            sql += "'1', ";
            sql += "1, ";
            sql += "10, ";
            sql += "0.0, ";
            sql += "0.0, ";
            sql += "0.0, ";
            sql += "0.25, ";
            sql += "25.0, ";
            sql += "0.1, ";
            sql += "0.1, ";
            sql += "'Y', ";
            sql += "'NONE', ";
            sql += "'1', ";
            sql += "0, ";
            sql += "0, ";
            sql += "0.0, ";
            sql += "0.0, ";
            sql += "0.0, ";
            sql += "0.25, ";
            sql += "0.0, ";
            sql += "0.0, ";
            sql += "0.0)";

            return sql;
        }
    }
}
