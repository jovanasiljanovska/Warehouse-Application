using System;
using System.Collections.Generic;
using System.Text;
using Warehouse.Domain.Domain.Enums;
using Warehouse.Domain.Identity;

namespace Warehouse.Domain.Domain
{
    public class Product : BaseEntity
    {
        public string Name { get; set; }
        public string? SKU { get; set; }

        public Guid CategoryId { get; set; }
        public  Category? Category { get; set; }
        public string? SupplierId { get; set; }
        public WarehouseApplicationUser? Supplier { get; set; }
        public string? ImageURL { get; set; }
        public int? UnitPrice { get; set; }

        public virtual ICollection<StockBalance>? StockBalances { get; set; }
    }
}
