using System;
using System.Collections.Generic;
using System.Text;
using Warehouse.Domain.Domain;
using Warehouse.Domain.Domain.Enums;

namespace Warehouse.Service.Interface;

public interface IPurchaseOrderService
{
    // Employee side
    List<PurchaseOrder> GetAll();
    PurchaseOrder? GetById(Guid id);
    PurchaseOrder CreatePurchaseOrder(string employeeId, string supplierId, Guid productId, int quantity);
    PurchaseOrder Receive(Guid purchaseOrderId, string employeeId, LocationType targetLocation);


    // Supplier side
    List<PurchaseOrder> GetIncomingForSupplier(string supplierId);
    PurchaseOrder Accept(Guid purchaseOrderId, string supplierId);
    PurchaseOrder Ship(Guid purchaseOrderId, string supplierId);
}


