using System;
using System.Collections.Generic;
using System.Text;
using Warehouse.Domain.Domain.Enums;

namespace Warehouse.Domain.Domain
{
    public class StockBalance : BaseEntity
    {
        public Guid ProductId { get; set; }
        public Product? Product { get; set; }

        public LocationType LocationType { get; set; }
        public int Quantity { get; set; }
    }
}
