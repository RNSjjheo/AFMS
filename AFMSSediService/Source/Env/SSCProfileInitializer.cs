using AFMSDll;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace AFMSSediService
{
    internal static class SSCProfileInitializer
    {
        private const string START_DATE = "20260901";
        public static bool EnsureDefaultProfile()
        {
            using FBDatabase db = FBProvider.Instance.CreateDatabase();

            string countSql = $"SELECT COUNT(*) FROM {FbtAFMSSediSSCProfile.TABLE_NAME}";
            string error = db.RunQuery(countSql);

            if (!string.IsNullOrEmpty(error))
            {
                throw new InvalidOperationException(
                    $"{FbtAFMSSediSSCProfile.TABLE_NAME} 테이블의 데이터 확인에 실패했습니다.\n{error}");
            }

            if (HasProfile(db.Results)) return false;

            error = db.RunNonQuery(CreateDefaultInsertSql());

            if (!string.IsNullOrEmpty(error))
            {
                throw new InvalidOperationException(
                    $"{FbtAFMSSediSSCProfile.TABLE_NAME} 기본값 저장에 실패했습니다.\n{error}");
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
            string sql = $"INSERT INTO {FbtAFMSSediSSCProfile.TABLE_NAME} (";
            sql += $"{FbtAFMSSediSSCProfile.COL_PROFILE_ID}, ";
            sql += $"{FbtAFMSSediSSCProfile.COL_PROFILE_DATE}, ";
            sql += $"{FbtAFMSSediSSCProfile.COL_PROFILE_TIME}, ";
            sql += $"{FbtAFMSSediSSCProfile.COL_PROFILE_NAME}, ";
            sql += $"{FbtAFMSSediSSCProfile.COL_DEVICE_TYPE}, ";
            sql += $"{FbtAFMSSediSSCProfile.COL_CELL_FROM}, ";
            sql += $"{FbtAFMSSediSSCProfile.COL_CELL_TO}, ";
            sql += $"{FbtAFMSSediSSCProfile.COL_K_VALUE}, ";
            sql += $"{FbtAFMSSediSSCProfile.COL_BEAM_ANGLE}, ";
            sql += $"{FbtAFMSSediSSCProfile.COL_SSC_A}, ";
            sql += $"{FbtAFMSSediSSCProfile.COL_SSC_B})";
            sql += " VALUES (";
            sql += "1, ";
            sql += $"'{START_DATE}', ";
            sql += "'000000', ";
            sql += $"'{START_DATE}_000000', ";
            sql += "'CM600', ";
            sql += "1, ";
            sql += "10, ";
            sql += "0.25, ";
            sql += "25.0, ";
            sql += "0.1, ";
            sql += "0.1)";

            return sql;
        }
    }
}
