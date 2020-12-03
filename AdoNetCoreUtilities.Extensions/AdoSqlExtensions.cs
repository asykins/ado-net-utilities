using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace AdoNetCoreUtilities.Extensions
{
    public static class AdoSqlExtensions
    {
        /// <summary>
        /// Get the value at the specified index or return the default.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataReader"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static T SafeGet<T>(this SqlDataReader dataReader, int index)
        {
            if (dataReader.IsDBNull(index))
                return default;
            else
                return (T)dataReader.GetValue(index);
        }

        /// <summary>
        /// Create a new parameter with the value passed. If the value is null, DBNull is set.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sqlDbCommand"></param>
        /// <param name="parameterName"></param>
        /// <param name="sqlDbType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IDbCommand SafeSet<T>
            (this SqlCommand sqlDbCommand, string parameterName, SqlDbType sqlDbType, T value)
        {
            if (value == null)
                sqlDbCommand.Parameters.Add($"@{parameterName}", sqlDbType).Value = DBNull.Value;
            else
                sqlDbCommand.Parameters.Add($"@{parameterName}", sqlDbType).Value = value;
            return sqlDbCommand;
        }
    }
}
