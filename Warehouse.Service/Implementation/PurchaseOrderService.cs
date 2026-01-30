using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Warehouse.Domain.Domain;
using Warehouse.Domain.Domain.Enums;
using Warehouse.Repository.Interface;
using Warehouse.Service.Interface;

namespace Warehouse.Service.Implementation;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IRepository<PurchaseOrder> _poRepo;
    private readonly IRepository<PurchaseProductInOrder> _lineRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly IInventoryService _inventory;

    public PurchaseOrderService(
        IRepository<PurchaseOrder> poRepo,
        IRepository<PurchaseProductInOrder> lineRepo,
        IRepository<Product> productRepo,
        IInventoryService inventory)
    {
        _poRepo = poRepo;
        _lineRepo = lineRepo;
        _productRepo = productRepo;
        _inventory = inventory;
    }

    // -----------------------
    // Employee/Admin side
    // -----------------------
    public List<PurchaseOrder> GetAll()
        => _poRepo.GetAll(
            selector: x => x,
            orderBy: q => q.OrderByDescending(o => o.DateCreated),
            include: q => q.Include(o => o.PurchaseProductInOrders)
                           .ThenInclude(l => l.OrderedProduct)
                           .Include(o => o.Supplier)
                           .Include(o => o.Employee)
                           
        ).ToList();

    public PurchaseOrder? GetById(Guid id)
        => _poRepo.Get(
            selector: x => x,
            predicate: o => o.Id == id,
            include: q => q.Include(o => o.PurchaseProductInOrders)
                           .ThenInclude(l => l.OrderedProduct)
                           .Include(o => o.Supplier)
                           .Include(o => o.Employee)
        );

    public PurchaseOrder CreatePurchaseOrder(string employeeId, string supplierId, Guid productId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(employeeId)) throw new Exception("EmployeeId is required.");
        if (string.IsNullOrWhiteSpace(supplierId)) throw new Exception("SupplierId is required.");
        if (quantity <= 0) throw new Exception("Quantity must be > 0.");

        var product = _productRepo.Get(selector: x => x, predicate: p => p.Id == productId)
                      ?? throw new Exception("Product not found.");

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            SupplierId = supplierId,
            Status = OrderStatus.Ordered,
            DateCreated = DateTime.UtcNow
        };

        _poRepo.Insert(po);

        var line = new PurchaseProductInOrder
        {
            Id = Guid.NewGuid(),
            OrderId = po.Id,
            ProductId = product.Id,
            Quantity = quantity
        };

        _lineRepo.Insert(line);

        return GetById(po.Id)!;
    }

    public PurchaseOrder Receive(Guid purchaseOrderId, string employeeId, LocationType targetLocation)
    {
        if (string.IsNullOrWhiteSpace(employeeId)) throw new Exception("EmployeeId is required.");

        if (targetLocation != LocationType.Shelves &&
            targetLocation != LocationType.Freezer )
            throw new Exception("Invalid put-away location.");

        var po = _poRepo.Get(
            selector: x => x,
            predicate: o => o.Id == purchaseOrderId,
            include: q => q.Include(o => o.PurchaseProductInOrders)
        ) ?? throw new Exception("Purchase order not found.");

        if (po.Status != OrderStatus.Shipped)
            throw new Exception("Only Shipped PO can be received.");

        foreach (var line in po.PurchaseProductInOrders ?? new List<PurchaseProductInOrder>())
        {
            _inventory.AddToReceiving(line.ProductId, line.Quantity);
            _inventory.PutAway(line.ProductId, line.Quantity, targetLocation);
        }

        po.Status = OrderStatus.Received;
        _poRepo.Update(po);

        return GetById(po.Id)!;
    }


    // -----------------------
    // Supplier side
    // -----------------------
    public List<PurchaseOrder> GetIncomingForSupplier(string supplierId)
        => _poRepo.GetAll(
            selector: x => x,
            predicate: po => po.SupplierId == supplierId &&
                             (po.Status == OrderStatus.Ordered || po.Status == OrderStatus.Approved),
            orderBy: q => q.OrderByDescending(po => po.DateCreated),
            include: q => q.Include(po => po.PurchaseProductInOrders)
                           .ThenInclude(l => l.OrderedProduct)
                           .Include(po => po.Employee)
        ).ToList();

    public PurchaseOrder Accept(Guid purchaseOrderId, string supplierId)
    {
        var po = _poRepo.Get(
            selector: x => x,
            predicate: x => x.Id == purchaseOrderId && x.SupplierId == supplierId,
            include: q => q.Include(x => x.PurchaseProductInOrders)
        ) ?? throw new Exception("Purchase order not found.");

        if (po.Status != OrderStatus.Ordered)
            throw new Exception("Only Ordered purchase orders can be accepted.");

        po.Status = OrderStatus.Approved;
        _poRepo.Update(po);

        return GetById(po.Id)!;
    }

    public PurchaseOrder Ship(Guid purchaseOrderId, string supplierId)
    {
        var po = _poRepo.Get(
            selector: x => x,
            predicate: x => x.Id == purchaseOrderId && x.SupplierId == supplierId,
            include: q => q.Include(x => x.PurchaseProductInOrders)
        ) ?? throw new Exception("Purchase order not found.");

        // If you don’t want Approved step, allow shipping from Ordered too.
        if (po.Status != OrderStatus.Ordered && po.Status != OrderStatus.Approved)
            throw new Exception("Only Ordered/Approved purchase orders can be shipped.");

        po.Status = OrderStatus.Shipped;
        _poRepo.Update(po);

        return GetById(po.Id)!;
    }
}


