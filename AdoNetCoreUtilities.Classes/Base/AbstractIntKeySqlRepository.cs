using AdoNetCoreUtilities.Domain.Base;
using Microsoft.Extensions.Configuration;

namespace AdoNetCoreUtilities.Classes.Base
{
    public abstract class AbstractIntKeySqlRepository<TEntity> : AbstractSqlRepository<TEntity, int>
        where TEntity : AdoEntityBase<int>, new()
    {
        protected AbstractIntKeySqlRepository(IConfiguration configuration) 
            : base(configuration) { }
    }
}
