﻿using System;

namespace AdoNetCoreUtilities.Domain.Attributes
{
    [System.AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class OrderAttribute : Attribute
    {
        public readonly int columnOrder ;

        public OrderAttribute(int columnOrder)
        {
            this.columnOrder = columnOrder;
        }
    }
}