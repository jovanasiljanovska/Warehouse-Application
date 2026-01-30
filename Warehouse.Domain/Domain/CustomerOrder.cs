using System;
using System.Collections.Generic;
using System.Text;
using Warehouse.Domain.Domain.Enums;
using Warehouse.Domain.Identity;

namespace Warehouse.Domain.Domain
{
    public class CustomerOrder : BaseEntity
    {
        public string CustomerId { get; set; }
        public WarehouseApplicationUser? Customer { get; set; }
        public ICollection<ProductInOrder>? ProductInOrders { get; set; }
        public DateTime DateCreated { get; set; }
        public OrderStatus Status { get; set; }
    }
}
