using System;
using System.Collections.Generic;
using System.Text;
using Warehouse.Domain.Domain;

namespace Warehouse.Service.Interface;

public interface ICustomerOrderService
{
    List<CustomerOrder> GetAll();
    CustomerOrder? GetById(Guid id);
    List<CustomerOrder> GetOrdersForCustomer(string customerId);

    CustomerOrder CreateOrder(string customerId, Guid productId, int quantity);
    CustomerOrder ShipOrder(Guid orderId, string performedBy);
    CustomerOrder CancelOrder(Guid orderId, string performedBy);
}


