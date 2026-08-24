using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class FBColumnSchema
    {
        public string Name { get; set; }

        public int Position { get; set; }

        public int FieldType { get; set; }

        public int FieldSubType { get; set; }

        /// <summary>
        /// 저장 Byte 길이
        /// </summary>
        public int? FieldLength { get; set; }

        /// <summary>
        /// CHAR / VARCHAR 문자 길이
        /// </summary>
        public int? CharacterLength { get; set; }

        public int? Precision { get; set; }

        public int? Scale { get; set; }

        public bool IsNullable { get; set; }

        public int? CharacterSetId { get; set; }

        public int? CollationId { get; set; }

        public string GetDisplayType()
        {
            // NUMERIC
            if (FieldSubType == 1 &&
                IsNumericStorageType(FieldType))
            {
                return BuildNumericType("NUMERIC");
            }

            // DECIMAL
            if (FieldSubType == 2 &&
                IsNumericStorageType(FieldType))
            {
                return BuildNumericType("DECIMAL");
            }

            switch (FieldType)
            {
                case 7:
                    return "SMALLINT";

                case 8:
                    return "INTEGER";

                case 10:
                    return "FLOAT";

                case 12:
                    return "DATE";

                case 13:
                    return "TIME";

                case 14:
                    return CharacterLength.HasValue
                        ? $"CHAR({CharacterLength.Value})"
                        : "CHAR";

                case 16:
                    return "BIGINT";

                case 23:
                    return "BOOLEAN";

                case 24:
                    return "DECFLOAT(16)";

                case 25:
                    return "DECFLOAT(34)";

                case 26:
                    return "INT128";

                case 27:
                    return "DOUBLE PRECISION";

                case 28:
                    return "TIME WITH TIME ZONE";

                case 29:
                    return "TIMESTAMP WITH TIME ZONE";

                case 35:
                    return "TIMESTAMP";

                case 37:
                    return CharacterLength.HasValue
                        ? $"VARCHAR({CharacterLength.Value})"
                        : "VARCHAR";

                case 261:

                    if (FieldSubType == 1)
                        return "BLOB SUB_TYPE TEXT";

                    return $"BLOB SUB_TYPE {FieldSubType}";

                default:
                    return $"UNKNOWN({FieldType})";
            }
        }

        private string BuildNumericType(string name)
        {
            if (!Precision.HasValue)
                return name;

            int scale = Math.Abs(Scale ?? 0);

            return $"{name}({Precision.Value},{scale})";
        }

        private static bool IsNumericStorageType(int fieldType)
        {
            return fieldType == 7     // SMALLINT
                || fieldType == 8    // INTEGER
                || fieldType == 16   // BIGINT
                || fieldType == 26;  // INT128
        }
    }
}
