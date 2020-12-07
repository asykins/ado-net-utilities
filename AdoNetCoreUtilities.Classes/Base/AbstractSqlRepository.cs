using AdoNetCoreUtilities.Domain.Attributes;
using AdoNetCoreUtilities.Domain.Base;
using AdoNetCoreUtilities.Domain.Interfaces;
using AdoNetCoreUtilities.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace AdoNetCoreUtilities.Classes.Base
{
    public abstract class AbstractSqlRepository<TEntity, TKey> : ISqlRepository<TEntity>
        where TEntity : AdoEntityBase<TKey>, new()
        where TKey : struct
    {
        private readonly IConfiguration configuration;
        protected abstract string ConfigurationConnectionStringKey { get; }
        protected abstract string SqlTableName { get; }

        public AbstractSqlRepository(IConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<IEnumerable<TEntity>> GetAsync()
        {
            var data = new List<TEntity>();

            using (var connection = new SqlConnection(configuration.GetConnectionString(ConfigurationConnectionStringKey)))
            {
                using (var command = connection.CreateCommand())
                {
                    SetGetCommand(command);

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            data.Add(Map(reader));
                        }

                        return data;
                    }
                }
            }
        }

        public async Task<IEnumerable<TEntity>> GetAsync(params Func<TEntity, bool>[] functions)
            => (await GetAsync()).AggregateWhereFunctions(functions);

        public async Task InsertOrUpdateAsync(IEnumerable<TEntity> source)
        {
            using (var connection = new SqlConnection(configuration.GetConnectionString(ConfigurationConnectionStringKey)))
            {
                using (var command = connection.CreateCommand())
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            command.Transaction = transaction;

                            var temporaryTableName = await CreateTemporarySqlTable();

                            await BulkInsert(source, temporaryTableName);

                            var properties = OrderAttribute.GetPropertiesOrder<TEntity>();

                            command.CommandText =
                                @$"MERGE {SqlTableName} AS TARGET
                           USING {temporaryTableName} AS SOURCE
                            ON TARGET.Id = Source.Id
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

                            await transaction.CommitAsync();
                        }
                        catch
                        {
                            await transaction.RollbackAsync();

                            throw;
                        }
                    }
                }
            }
        }

        public async Task BulkInsert(IEnumerable<TEntity> source, string tableName = null)
        {
            using (var connection = new SqlConnection(configuration.GetConnectionString(ConfigurationConnectionStringKey)))
            {
                using (var sqlBulkCopy = new SqlBulkCopy(connection))
                {
                    var orderedProperties = OrderAttribute.GetPropertiesOrder<TEntity>();

                    var dataTable = FillDataTable(source, orderedProperties);

                    SetBulkCopy(sqlBulkCopy, orderedProperties, tableName);

                    await connection.OpenAsync();

                    await sqlBulkCopy.WriteToServerAsync(dataTable);
                }
            }
        }

        protected virtual void SetGetCommand(SqlCommand command)
            => command.CommandText = @$"SELECT * FROM {SqlTableName}";

        protected virtual TEntity Map(SqlDataReader reader)
        {
            var mappedData = new TEntity();

            typeof(TEntity).GetProperties().ToList().ForEach(x =>
            {
                mappedData.GetType()
                    .GetProperty(x.Name)
                    .SetValue(mappedData, reader.SafeGet<object>(OrderAttribute.GetOrderAttributeValue<TEntity>(x.Name)));
            });

            return mappedData;
        }

        private void SetBulkCopy(SqlBulkCopy sqlBulkCopy, IOrderedEnumerable<KeyValuePair<int, string>> properties, string tableName = null)
        {
            sqlBulkCopy.DestinationTableName = tableName ?? SqlTableName;

            for (var index = 0; index < properties.Count(); index++)
            {
                sqlBulkCopy.ColumnMappings.Add(properties.ElementAt(index).Value, properties.ElementAt(index).Value);
            }

        }

        private DataTable FillDataTable(IEnumerable<TEntity> source, IOrderedEnumerable<KeyValuePair<int, string>> properties)
        {
            var dataTable = new DataTable();

            var baseSource = source.FirstOrDefault() ?? new TEntity();

            foreach (var property in properties)
            {
                dataTable = dataTable.AddDataColumn(property.Value, baseSource);
            }

            foreach (var item in source)
            {
                var dataRow = dataTable.NewRow();

                foreach (var property in properties)
                {
                    dataRow = dataRow.AddDataRowValues(property.Value, item.GetType().GetProperty(property.Value).GetValue(item));
                }

                dataTable.Rows.Add(dataRow);
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
