using System;
using System.Collections.Generic;
using System.Linq;

namespace AdoNetCoreUtilities.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<TEntity> AggregateWhereFunctions<TEntity>(this IEnumerable<TEntity> source, IEnumerable<Func<TEntity, bool>> functions)
            => functions.Aggregate(source, (source, query) => source.Where(query));
    }
}
