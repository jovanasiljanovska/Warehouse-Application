using System;
using System.Collections.Generic;
using System.Text;
using Warehouse.Domain.Domain.Enums;
using Warehouse.Domain.Identity;

namespace Warehouse.Domain.Domain
{
    public class PurchaseOrder : BaseEntity
    {
       public string EmployeeId { get; set; }
        public WarehouseApplicationUser? Employee { get; set; }
       public ICollection<PurchaseProductInOrder>? PurchaseProductInOrders { get; set; }
        public string SupplierId { get; set; }
        public WarehouseApplicationUser? Supplier { get; set; }
        public DateTime DateCreated { get; set; }
        public OrderStatus Status { get; set; }

    }
}
