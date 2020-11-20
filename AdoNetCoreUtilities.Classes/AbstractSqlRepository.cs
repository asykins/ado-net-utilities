using AdoNetCoreUtilities.Domain.Interfaces;
using AdoNetCoreUtilities.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace AdoNetCoreUtilities.Classes
{
    public abstract class AbstractSqlRepository<T> : IAbstractSqlRepository<T>
        where T: class, new()
    {
        private readonly IConfiguration configuration;
        protected abstract string ConfigurationConnectionStringKey { get; }
        protected abstract string SqlTableName { get; }

        public AbstractSqlRepository(IConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<IEnumerable<T>> GetAsync()
        {
            var data = new List<T>();

            using(var connection = new SqlConnection(configuration.GetConnectionString(ConfigurationConnectionStringKey)))
            {
                using(var command = connection.CreateCommand())
                {
                    SetGetCommand(command);

                    await connection.OpenAsync();

                    using(var reader = await command.ExecuteReaderAsync())
                    {
                        while(await reader.ReadAsync())
                        {
                            data.Add(Map(reader));
                        }

                        return data;
                    }
                }
            }
        }

        public async Task<IEnumerable<T>> GetAsync(params Func<T, bool>[] functions)
            => AggregateWhereFunctions(await GetAsync(), functions);

        public async Task InsertOrUpdateAsync(IEnumerable<T> source)
        {
            using (var connection = new SqlConnection(configuration.GetConnectionString(ConfigurationConnectionStringKey)))
            {
                using (var command = connection.CreateCommand())
                {
                    var temporaryTableName = await CreateTemporarySqlTable();

                    await BulkInsert(source, temporaryTableName);

                    var properties = OrderAttributeExtensions.GetPropertiesOrder<T>();

                    command.CommandText =
                        @$"MERGE {SqlTableName} AS TARGET
                           USING {temporaryTableName} AS SOURCE
                            ON TARGET.Id == Source.Id
                           WHEN MATCHED AND {properties.Select(x => $"TARGET.{x.Value} <> SOURCE.{x.Value}")
                                                .Aggregate((current, previous) => $"{current} OR {previous}")}
                            THEN 
                                UPDATE SET {properties.Select(x => $"TARGET.{x.Value} = SOURCE.{x.Value}")
                                                .Aggregate((current, previous) => $"{current}, {previous}")}
                           WHEN NOT MATCHED
                            THEN
                                INSERT ({properties.Select(x => x.Value).Aggregate((current, previous) => $"{current}, {previous}")})
                                VALUES ({properties.Select(x => $"SOURCE.{x.Value}").Aggregate((current, previous) => $"{current}, {previous}")})
                            ;";

                    await command.ExecuteNonQueryAsync();

                    await DropTable(temporaryTableName);
                }
            }
        }

        public async Task BulkInsert(IEnumerable<T> source, string tableName = null)
        {
            using (var connection = new SqlConnection(configuration.GetConnectionString(ConfigurationConnectionStringKey)))
            {
                using (var sqlBulkCopy = new SqlBulkCopy(connection))
                {
                    var orderedProperties = OrderAttributeExtensions.GetPropertiesOrder<T>();

                    var dataTable = FillDataTable(source, orderedProperties);

                    SetBulkCopy(sqlBulkCopy, orderedProperties, tableName);

                    await connection.OpenAsync();

                    await sqlBulkCopy.WriteToServerAsync(dataTable);
                }
            }
        }

        protected virtual void SetGetCommand(SqlCommand command)
            => command.CommandText = @$"SELECT * FROM {SqlTableName}";

        protected virtual T Map(SqlDataReader reader) 
        {
            var mappedData = new T();

            typeof(T).GetProperties().ToList().ForEach(x =>
            {
                mappedData.GetType()
                    .GetProperty(x.Name)
                    .SetValue(mappedData, reader.SafeGet<object>(OrderAttributeExtensions.GetOrderAttributeValue<T>(x.Name)));
            });

            return mappedData;
        }

        protected IEnumerable<T> AggregateWhereFunctions(IEnumerable<T> source, IEnumerable<Func<T, bool>> functions)
            => functions.Aggregate(source, (source, query) => source.Where(query));

        private void SetBulkCopy(SqlBulkCopy sqlBulkCopy, IOrderedEnumerable<KeyValuePair<int, string>> properties, string tableName = null)
        {
            sqlBulkCopy.DestinationTableName = tableName ?? SqlTableName;

            for(var index = 0; index < properties.Count(); index++)
            {
                sqlBulkCopy.ColumnMappings.Add(properties.ElementAt(index).Value, properties.ElementAt(index).Value);
            }

        }

        private DataTable FillDataTable(IEnumerable<T> source, IOrderedEnumerable<KeyValuePair<int, string>> properties)
        {
            var dataTable = new DataTable();

            var baseSource = source.FirstOrDefault() ?? new T();

            foreach (var property in properties)
            {
                dataTable = dataTable.AddDataColumn(property.Value, baseSource);
            }

            foreach(var item in source)
            {
                foreach(var property in properties)
                {
                    dataTable = dataTable.AddDataRow(property.Value, item.GetType().GetProperty(property.Value).GetValue(item));
                }
            }

            return dataTable;
        }

        private async Task<string> CreateTemporarySqlTable()
        {
            using (var connection = new SqlConnection(configuration.GetConnectionString(ConfigurationConnectionStringKey)))
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        @$"SELECT TOP(0) * INTO __Temp_Table_{SqlTableName}_Source FROM {SqlTableName}";

                    await connection.OpenAsync();

                    await command.ExecuteNonQueryAsync();

                    return $"__Temp_Table_{SqlTableName}_Source";
                }
            }
        }

        private async Task DropTable(string tableName)
        {
            using (var connection = new SqlConnection(configuration.GetConnectionString(ConfigurationConnectionStringKey)))
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        @$"DROP TABLE {tableName ?? SqlTableName}";

                    await connection.OpenAsync();

                    await command.ExecuteNonQueryAsync();
                }
            }

        }
    }
}
