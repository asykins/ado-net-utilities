using System;
using System.Data;

namespace AdoNetCoreUtilities.Extensions
{
    public static class DatatableExtensions
    {
        public static DataTable AddDataColumn<T>(this DataTable dataTable, string columnName)
        {
            dataTable.Columns.Add(new DataColumn(columnName, typeof(T)));
            return dataTable;
        }

        public static DataTable AddDataColumn<T>(this DataTable dataTable, string columnName, T source)
        {
            dataTable.Columns.Add(new DataColumn(columnName, source.GetType().GetProperty(columnName).PropertyType));
            return dataTable;
        }

        public static DataTable AddDataRow<T>(this DataTable dataTable, string columnName, T value)
        {
            var dataRow = dataTable.NewRow();

            if (value == null)
                dataRow[columnName] = DBNull.Value;
            else
                dataRow[columnName] = value;

            dataTable.Rows.Add(dataRow);

            return dataTable;
        }
    }
}
