using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdoNetCoreUtilities.Domain.Interfaces
{
    public interface ISqlRepository<T>
    {
        Task<IEnumerable<T>> GetAsync();
        Task<IEnumerable<T>> GetAsync(params Func<T, bool>[] functions);
        Task InsertOrUpdateAsync(IEnumerable<T> source);
        Task BulkInsert(IEnumerable<T> source, string tableName = null);
    }
}
