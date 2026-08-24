using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public enum SurveyDifferenceType
    {
        QueryError,

        TableMissingRemote,
        TableMissingLocal,

        ColumnCountMismatch,

        ColumnMissingRemote,
        ColumnMissingLocal,

        ColumnPositionMismatch,

        ColumnTypeMismatch,
        ColumnLengthMismatch,
        CharacterLengthMismatch,

        PrecisionMismatch,
        ScaleMismatch,

        NullabilityMismatch,

        CharacterSetMismatch,
        CollationMismatch,

        ExistNotDate,
        ExistNotTime,
    }

    public class FBSurveyDifference
    {
        public SurveyDifferenceType Type { get; set; }

        public string ColumnName { get; set; } = "";

        public string RemoteValue { get; set; } = "";

        public string LocalValue { get; set; } = "";

        public override string ToString()
        {
            string target =
                string.IsNullOrWhiteSpace(ColumnName)
                    ? ""
                    : $" Column=[{ColumnName}]";

            return
                $"{Type}{target} " +
                $"Remote=[{RemoteValue}] " +
                $"Local=[{LocalValue}]";
        }
    }
}
