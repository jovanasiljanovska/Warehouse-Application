using System;
using System.Collections.Generic;
using System.Text;

namespace Warehouse.Domain.Domain
{
    public class PurchaseProductInOrder : BaseEntity
    {
        public Guid ProductId { get; set; }
        public Product? OrderedProduct { get; set; }

        public Guid OrderId { get; set; }
        public PurchaseOrder? Order { get; set; }
        public int Quantity { get; set; }
    }
}
