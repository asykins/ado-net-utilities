using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdoNetCoreUtilities.Domain.Interfaces
{
    public interface ISqlRepository<TEntity>
    {
        Task<IEnumerable<TEntity>> GetAsync();
        Task<IEnumerable<TEntity>> GetAsync(params Func<TEntity, bool>[] functions);
        Task InsertOrUpdateAsync(IEnumerable<TEntity> source);
        Task BulkInsert(IEnumerable<TEntity> source, string tableName = null);
    }
}
