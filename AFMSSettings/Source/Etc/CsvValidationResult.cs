using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSSettings
{
    public class CsvValidationResult
    {
        public bool IsValid { get; init; }
        public int PointCount { get; init; }
        public string Message { get; init; } = string.Empty;

        public static CsvValidationResult Success(int pointCount)
        {
            return new CsvValidationResult
            {
                IsValid = true,
                PointCount = pointCount,
                Message = "정상적인 단면 데이터 파일입니다."
            };
        }

        public static CsvValidationResult Fail(string message)
        {
            return new CsvValidationResult
            {
                IsValid = false,
                Message = message
            };
        }
    }
}
