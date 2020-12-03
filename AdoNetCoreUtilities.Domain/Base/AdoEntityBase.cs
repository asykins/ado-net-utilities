namespace AdoNetCoreUtilities.Domain.Base
{
    public class AdoEntityBase<TKey> where TKey: struct
    {
        public TKey Id { get; set; }
    }
}
