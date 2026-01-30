using System;
using System.Collections.Generic;
using System.Text;
using Warehouse.Domain.Domain;
using Warehouse.Domain.Identity;

namespace Warehouse.Domain.Domain
{
    public class ShoppingCart : BaseEntity
    {
        public string UserId { get; set; }
        public WarehouseApplicationUser? User { get; set; }

        public ICollection<ProductInShoppingCart>? Items { get; set; }
    }
}

