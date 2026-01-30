using System;
using System.Collections.Generic;
using System.Text;

namespace Warehouse.Domain.Domain
{
    public class ProductInOrder : BaseEntity
    {
        public Guid ProductId { get; set; }
        public Product? OrderedProduct { get; set; }

        public Guid OrderId { get; set; }
        public CustomerOrder? Order { get; set; }
        public int Quantity { get; set; }
    }
}
