using AFMSDll;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace AFMSDll
{
    public class FBCompareTable
    {
        private readonly FBDatabase _dbRemote;
        private readonly FBDatabase _dbLocal;

        public string _dbTableName;

        public FBCompareTable(FBDatabase remote, FBDatabase local, string tablename)
        {
            _dbRemote = remote;
            _dbLocal = local;
            _dbTableName = tablename;
        }

        /// <summary>
        /// Remote / Local 테이블 구조 비교
        /// </summary>
        public FBSurveyTableValidationResult Validate()
        {
            FBSurveyTableValidationResult result = new FBSurveyTableValidationResult(_dbTableName);

            FBTableSchema remoteSchema = ReadTableSchema(_dbRemote);
            FBTableSchema localSchema = ReadTableSchema(_dbLocal);

            result.RemoteTableExists = remoteSchema.Exists;
            result.LocalTableExists = localSchema.Exists;

            // Remote DB 조회 오류
            if (!string.IsNullOrWhiteSpace(remoteSchema.Error))
            {
                result.Differences.Add(new FBSurveyDifference
                {
                    Type = SurveyDifferenceType.QueryError,
                    ColumnName = "",
                    RemoteValue = remoteSchema.Error,
                    LocalValue = ""
                });

                return result;
            }

            // Local DB 조회 오류
            if (!string.IsNullOrWhiteSpace(localSchema.Error))
            {
                result.Differences.Add(new FBSurveyDifference
                {
                    Type = SurveyDifferenceType.QueryError,
                    ColumnName = "",
                    RemoteValue = "",
                    LocalValue = localSchema.Error
                });

                return result;
            }

            // --------------------------------------------------
            // 테이블 존재 여부
            // --------------------------------------------------

            if (!remoteSchema.Exists)
            {
                result.Differences.Add(new FBSurveyDifference
                {
                    Type = SurveyDifferenceType.TableMissingRemote,
                    ColumnName = "",
                    RemoteValue = "NOT EXISTS",
                    LocalValue = localSchema.Exists ? "EXISTS" : "NOT EXISTS"
                });
            }

            if (!localSchema.Exists)
            {
                result.Differences.Add(new FBSurveyDifference
                {
                    Type = SurveyDifferenceType.TableMissingLocal,
                    ColumnName = "",
                    RemoteValue = remoteSchema.Exists ? "EXISTS" : "NOT EXISTS",
                    LocalValue = "NOT EXISTS"
                });
            }

            // 한쪽이라도 테이블이 없으면 컬럼 비교 불가능
            if (!remoteSchema.Exists || !localSchema.Exists) return result;

            // --------------------------------------------------
            // Column 개수
            // --------------------------------------------------

            if (remoteSchema.Columns.Count != localSchema.Columns.Count)
            {
                result.Differences.Add(new FBSurveyDifference
                {
                    Type = SurveyDifferenceType.ColumnCountMismatch,
                    ColumnName = "",
                    RemoteValue = remoteSchema.Columns.Count.ToString(),
                    LocalValue = localSchema.Columns.Count.ToString()
                });
            }

            // --------------------------------------------------
            // Column 이름 기준 비교
            // --------------------------------------------------

            Dictionary<string, FBColumnSchema> remoteColumns = remoteSchema.Columns.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
            Dictionary<string, FBColumnSchema> localColumns = localSchema.Columns.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            HashSet<string> columnNames = new HashSet<string>(remoteColumns.Keys,StringComparer.OrdinalIgnoreCase);

            columnNames.UnionWith(localColumns.Keys);

            foreach (string columnName in columnNames)
            {
                bool remoteExists = remoteColumns.TryGetValue(columnName, out FBColumnSchema remote);

                bool localExists = localColumns.TryGetValue(columnName, out FBColumnSchema local);

                // Remote에 없음
                if (!remoteExists)
                {
                    result.Differences.Add(new FBSurveyDifference
                    {
                        Type = SurveyDifferenceType.ColumnMissingRemote,
                        ColumnName = columnName,
                        RemoteValue = "NOT EXISTS",
                        LocalValue = local.GetDisplayType()
                    });

                    continue;
                }

                // Local에 없음
                if (!localExists)
                {
                    result.Differences.Add(new FBSurveyDifference
                    {
                        Type = SurveyDifferenceType.ColumnMissingLocal,
                        ColumnName = columnName,
                        RemoteValue = remote.GetDisplayType(),
                        LocalValue = "NOT EXISTS"
                    });

                    continue;
                }

                CompareColumn(result, remote, local);
            }

            HasMeasureDateTimeColumns(result, _FBTableBase.COL_MEASURE_DATE);
            HasMeasureDateTimeColumns(result, _FBTableBase.COL_MEASURE_TIME);

            return result;
        }

        /// <summary>
        /// 개별 Column 비교
        /// </summary>
        private void CompareColumn(FBSurveyTableValidationResult result, FBColumnSchema remote, FBColumnSchema local)
        {
            // Column 순서
            //if (remote.Position != local.Position)
            //{
            //    AddDifference(result, SurveyDifferenceType.ColumnPositionMismatch, remote.Name, remote.Position, local.Position);
            //}

            // Field Type
            if (remote.FieldType != local.FieldType || remote.FieldSubType != local.FieldSubType)
            {
                AddDifference(result, SurveyDifferenceType.ColumnTypeMismatch, remote.Name, remote.GetDisplayType(), local.GetDisplayType());
            }

            // 실제 저장 Byte 길이
            if (remote.FieldLength != local.FieldLength)
            {
                AddDifference(result, SurveyDifferenceType.ColumnLengthMismatch, remote.Name, remote.FieldLength, local.FieldLength);
            }

            // CHAR / VARCHAR 문자 길이
            if (remote.CharacterLength != local.CharacterLength)
            {
                AddDifference(result, SurveyDifferenceType.CharacterLengthMismatch, remote.Name, remote.CharacterLength, local.CharacterLength);
            }

            // NUMERIC / DECIMAL precision
            if (remote.Precision != local.Precision)
            {
                AddDifference(result, SurveyDifferenceType.PrecisionMismatch, remote.Name, remote.Precision, local.Precision);
            }

            // NUMERIC / DECIMAL scale
            if (remote.Scale != local.Scale)
            {
                AddDifference(result, SurveyDifferenceType.ScaleMismatch, remote.Name, remote.Scale, local.Scale);
            }

            // NULL / NOT NULL
            if (remote.IsNullable != local.IsNullable)
            {
                AddDifference(result, SurveyDifferenceType.NullabilityMismatch, remote.Name, remote.IsNullable ? "NULL" : "NOT NULL", local.IsNullable ? "NULL" : "NOT NULL");
            }

            // Character Set
            if (remote.CharacterSetId != local.CharacterSetId)
            {
                AddDifference(result, SurveyDifferenceType.CharacterSetMismatch, remote.Name, remote.CharacterSetId, local.CharacterSetId);
            }

            // Collation
            if (remote.CollationId != local.CollationId)
            {
                AddDifference(result, SurveyDifferenceType.CollationMismatch, remote.Name, remote.CollationId, local.CollationId);
            }
        }

        private void AddDifference(FBSurveyTableValidationResult result, SurveyDifferenceType type, string columnName, object remoteValue, object localValue)
        {
            result.Differences.Add(new FBSurveyDifference
            {
                Type = type,
                ColumnName = columnName,
                RemoteValue = ValueToString(remoteValue),
                LocalValue = ValueToString(localValue)
            });
        }

        /// <summary>
        /// DB로부터 Table Schema 읽기
        /// </summary>
        private FBTableSchema ReadTableSchema(FBDatabase db)
        {
            FBTableSchema schema = new FBTableSchema();

            string tableName = EscapeSqlLiteral(_dbTableName.Trim());

            // --------------------------------------------------
            // Table 존재 확인
            // --------------------------------------------------

            string tableCheckSql = $@"
SELECT FIRST 1
       TRIM(RDB$RELATION_NAME) AS TABLE_NAME
FROM RDB$RELATIONS
WHERE UPPER(TRIM(RDB$RELATION_NAME)) = UPPER('{tableName}')
  AND COALESCE(RDB$SYSTEM_FLAG, 0) = 0
  AND RDB$VIEW_BLR IS NULL
";

            string error = db.RunQuery(tableCheckSql);

            if (!string.IsNullOrWhiteSpace(error))
            {
                schema.Error = error;
                return schema;
            }

            // FBDatabase.Results는 다음 RunQuery에서 교체되므로
            // 필요한 결과를 바로 사용
            schema.Exists = db.Results.Rows.Count > 0;

            if (!schema.Exists)
                return schema;

            // --------------------------------------------------
            // Column Metadata
            // --------------------------------------------------

            string columnSql = $@"
SELECT
    TRIM(RF.RDB$FIELD_NAME)             AS COLUMN_NAME,

    RF.RDB$FIELD_POSITION               AS COLUMN_POSITION,

    F.RDB$FIELD_TYPE                    AS FIELD_TYPE,

    COALESCE(F.RDB$FIELD_SUB_TYPE, 0)   AS FIELD_SUB_TYPE,

    F.RDB$FIELD_LENGTH                  AS FIELD_LENGTH,

    F.RDB$CHARACTER_LENGTH              AS CHAR_LENGTH_VALUE,

    F.RDB$FIELD_PRECISION               AS FIELD_PRECISION,

    F.RDB$FIELD_SCALE                   AS FIELD_SCALE,

    COALESCE(RF.RDB$NULL_FLAG, 0)       AS NULL_FLAG,

    F.RDB$CHARACTER_SET_ID              AS CHARACTER_SET_ID,

    COALESCE(
        RF.RDB$COLLATION_ID,
        F.RDB$COLLATION_ID
    )                                   AS COLLATION_ID

FROM RDB$RELATION_FIELDS RF

INNER JOIN RDB$FIELDS F
    ON F.RDB$FIELD_NAME = RF.RDB$FIELD_SOURCE

WHERE UPPER(TRIM(RF.RDB$RELATION_NAME)) = UPPER('{tableName}')

ORDER BY RF.RDB$FIELD_POSITION
";

            error = db.RunQuery(columnSql);

            if (!string.IsNullOrWhiteSpace(error))
            {
                schema.Error = error;
                return schema;
            }

            // 다음 쿼리의 영향을 받지 않도록 Copy
            DataTable dt = db.Results.Copy();

            try
            {
                foreach (DataRow row in dt.Rows)
                {
                    FBColumnSchema column = new FBColumnSchema
                    {
                        Name = GetString(row, "COLUMN_NAME"),

                        Position = GetInt(row, "COLUMN_POSITION"),

                        FieldType = GetInt(row, "FIELD_TYPE"),

                        FieldSubType = GetInt(row, "FIELD_SUB_TYPE"),

                        FieldLength = GetNullableInt(row, "FIELD_LENGTH"),

                        CharacterLength = GetNullableInt(row, "CHAR_LENGTH_VALUE"),

                        Precision = GetNullableInt(row, "FIELD_PRECISION"),

                        Scale = GetNullableInt(row, "FIELD_SCALE"),

                        IsNullable = GetInt(row, "NULL_FLAG") != 1,

                        CharacterSetId = GetNullableInt(row, "CHARACTER_SET_ID"),

                        CollationId = GetNullableInt(row, "COLLATION_ID")
                    };

                    schema.Columns.Add(column);
                }
            }
            finally
            {
                dt.Dispose();
            }

            return schema;
        }

        private static string EscapeSqlLiteral(string value)
        {
            return value.Replace("'", "''");
        }

        private static string GetString(DataRow row, string columnName)
        {
            if (row[columnName] == DBNull.Value) return "";

            return Convert.ToString(row[columnName])?.Trim() ?? "";
        }

        private static int GetInt(DataRow row, string columnName)
        {
            if (row[columnName] == DBNull.Value) return 0;

            return Convert.ToInt32(row[columnName]);
        }

        private static int? GetNullableInt(DataRow row, string columnName)
        {
            if (row[columnName] == DBNull.Value) return null;

            return Convert.ToInt32(row[columnName]);
        }

        private static string ValueToString(object value)
        {
            if (value == null)return "NULL";

            return value.ToString() ?? "";
        }

        private void HasMeasureDateTimeColumns(FBSurveyTableValidationResult result, string essential)
        {
            FBTableSchema remoteSchema = ReadTableSchema(_dbRemote);
            FBTableSchema localSchema = ReadTableSchema(_dbLocal);

            if (!remoteSchema.Exists || !localSchema.Exists) return;
            if (!string.IsNullOrWhiteSpace(remoteSchema.Error) || !string.IsNullOrWhiteSpace(localSchema.Error)) return;

            bool remoteValid = HasMeasureDateTimeColumns(remoteSchema, essential);
            bool localValid = HasMeasureDateTimeColumns(localSchema, essential);

            if (!remoteValid || !localValid)
            {
                SurveyDifferenceType type = essential == _FBTableBase.COL_MEASURE_DATE ? SurveyDifferenceType.ExistNotDate : SurveyDifferenceType.ExistNotTime;

                result.Differences.Add(new FBSurveyDifference
                {
                    Type = type,
                    ColumnName = essential,
                    RemoteValue = "",
                    LocalValue = ""
                });
            }
        }

        private static bool HasMeasureDateTimeColumns(FBTableSchema schema, string essential)
        {
            bool hasMeasure= schema.Columns.Any(x => string.Equals(x.Name, essential, StringComparison.OrdinalIgnoreCase));

            return hasMeasure;
        }
    }
}
