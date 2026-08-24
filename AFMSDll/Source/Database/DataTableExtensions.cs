using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace AFMSDll
{
    public static class DataTableExtensions
    {
        public static DataTable AddRowNo(this DataTable table, string columnName = "No.")
        {
            if (!table.Columns.Contains(columnName))
            {
                table.Columns.Add(columnName, typeof(int));
                table.Columns[columnName]!.SetOrdinal(0);
            }

            for (int i = 0; i < table.Rows.Count; i++) table.Rows[i][columnName] = i + 1;

            return table;
        }
    }

    public static class ObjectExtensions
    {
        public static int ToInt(this object? value, int defaultValue = 0)
        {
            if (value == null || value == DBNull.Value) return defaultValue;
            return Convert.ToInt32(value);
        }

        public static double ToDouble(this object? value, double defaultValue = 0)
        {
            if (value == null || value == DBNull.Value) return defaultValue;
            return Convert.ToDouble(value);
        }

        public static string ToText(this object? value, string defaultValue = "")
        {
            if (value == null || value == DBNull.Value) return defaultValue;
            return Convert.ToString(value)?.Trim() ?? defaultValue;
        }
    }

}
