using System;
using System.Collections.Generic;
using System.Linq;

namespace AdoNetCoreUtilities.Domain.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class OrderAttribute : Attribute
    {
        public readonly int columnOrder;

        public OrderAttribute(int columnOrder)
        {
            this.columnOrder = columnOrder;
        }

        public static int GetOrderAttributeValue<T>(string propertyName)
        {
            if (!(typeof(T).GetProperty(propertyName).GetCustomAttributes(typeof(OrderAttribute), false).FirstOrDefault() is OrderAttribute orderAttribute))
                throw new ApplicationException($"No OrderAttribute has been applied to the selected property: {propertyName}");

            return orderAttribute.columnOrder;
        }

        public static IOrderedEnumerable<KeyValuePair<int, string>> GetPropertiesOrder<T>()
        {
            var propertiesOrder = new Dictionary<int, string>();

            typeof(T).GetProperties().ToList().ForEach(x =>
            {
                if (!(x.GetCustomAttributes(typeof(OrderAttribute), false).FirstOrDefault() is OrderAttribute orderAttribute))
                    throw new ApplicationException($"No OrderAttribute has been applied to the selected property: {x.Name}");

                propertiesOrder.Add(orderAttribute.columnOrder, x.Name);
            });

            return propertiesOrder.OrderBy(x => x.Key);
        }
    }
}
