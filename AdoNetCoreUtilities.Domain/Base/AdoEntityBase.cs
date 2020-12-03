using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdoNetCoreUtilities.Domain.Base
{
    public class AdoEntityBase<TKey> where TKey: struct
    {
        public TKey Id { get; set; }
    }
}
