using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class FBSurveyTableValidationResult
    {
        public string TableName { get; }

        public bool RemoteTableExists { get; internal set; }

        public bool LocalTableExists { get; internal set; }

        public List<FBSurveyDifference> Differences { get; } = new List<FBSurveyDifference>();

        public bool IsValid
        {
            get { return Differences.Count == 0; }
        }

        public FBSurveyTableValidationResult(string tableName)
        {
            TableName = tableName;
        }

        public override string ToString()
        {
            if (IsValid)
            {
                return $"[{TableName}] OK";
            }

            return
                $"[{TableName}] FAIL - " +
                $"{Differences.Count} difference(s)";
        }
    }
}
