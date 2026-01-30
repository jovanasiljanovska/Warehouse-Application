using System;
using System.Collections.Generic;
using System.Text;
using Warehouse.Domain.Domain;

namespace Warehouse.Domain.Domain
{
    public class ProductInShoppingCart : BaseEntity
    {
        public Guid ProductId { get; set; }
        public Product? Product { get; set; }

        public Guid ShoppingCartId { get; set; }
        public ShoppingCart? ShoppingCart { get; set; }

        public int Quantity { get; set; }
    }
}

