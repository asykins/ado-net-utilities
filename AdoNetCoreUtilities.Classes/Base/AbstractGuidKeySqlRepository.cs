using AdoNetCoreUtilities.Domain.Base;
using Microsoft.Extensions.Configuration;
using System;

namespace AdoNetCoreUtilities.Classes.Base
{
    public abstract class AbstractGuidKeySqlRepository<TEntity> : AbstractSqlRepository<TEntity, Guid>
        where TEntity : AdoEntityBase<Guid>, new()
    {
        protected AbstractGuidKeySqlRepository(IConfiguration configuration) 
            : base(configuration) { }
    }
}
