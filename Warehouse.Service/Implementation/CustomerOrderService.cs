using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Warehouse.Domain.Domain;
using Warehouse.Domain.Domain.Enums;
using Warehouse.Repository.Interface;
using Warehouse.Service.Interface;

namespace Warehouse.Service.Implementation;

public class CustomerOrderService : ICustomerOrderService
{
    private readonly IRepository<CustomerOrder> _orderRepo;
    private readonly IRepository<ProductInOrder> _lineRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly IInventoryService _inventory;

    public CustomerOrderService(
        IRepository<CustomerOrder> orderRepo,
        IRepository<ProductInOrder> lineRepo,
        IRepository<Product> productRepo,
        IInventoryService inventory)
    {
        _orderRepo = orderRepo;
        _lineRepo = lineRepo;
        _productRepo = productRepo;
        _inventory = inventory;
    }

    public List<CustomerOrder> GetAll()
        => _orderRepo.GetAll(
            selector: x => x,
            orderBy: q => q.OrderByDescending(o => o.DateCreated),
            include: q => q.Include(o => o.ProductInOrders)
                          .ThenInclude(l => l.OrderedProduct)
                          .Include(o => o.Customer)
        ).ToList();

    public CustomerOrder? GetById(Guid id)
        => _orderRepo.Get(
            selector: x => x,
            predicate: o => o.Id == id,
            include: q => q.Include(o => o.ProductInOrders)
                          .ThenInclude(l => l.OrderedProduct)
                          .Include(o => o.Customer)
        );

    public List<CustomerOrder> GetOrdersForCustomer(string customerId)
        => _orderRepo.GetAll(
            selector: x => x,
            predicate: o => o.CustomerId == customerId,
            orderBy: q => q.OrderByDescending(o => o.DateCreated),
            include: q => q.Include(o => o.ProductInOrders)
                          .ThenInclude(l => l.OrderedProduct)
        ).ToList();

    public CustomerOrder CreateOrder(string customerId, Guid productId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(customerId)) throw new Exception("CustomerId is required.");
        if (quantity <= 0) throw new Exception("Quantity must be > 0.");

        var product = _productRepo.Get(selector: x => x, predicate: p => p.Id == productId)
                      ?? throw new Exception("Product not found.");

        // move stock to shipping (Shelves/Freezer -> Shipping)
        _inventory.MoveToShipping(productId, quantity);

        var order = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Ordered,
            DateCreated = DateTime.UtcNow
        };

        _orderRepo.Insert(order);

        var line = new ProductInOrder
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = product.Id,
            Quantity = quantity
        };

        _lineRepo.Insert(line);

        return GetById(order.Id)!;
    }

    public CustomerOrder ShipOrder(Guid orderId, string performedBy)
    {
        if (string.IsNullOrWhiteSpace(performedBy)) throw new Exception("PerformedBy is required.");

        var order = _orderRepo.Get(selector: x => x,
        predicate: o => o.Id == orderId,
        include: q => q.Include(o => o.ProductInOrders)
                      .ThenInclude(l => l.OrderedProduct)
    ) ?? throw new Exception("Order not found.");

        if (order.Status != OrderStatus.Ordered)
            throw new Exception("Only Ordered orders can be shipped.");

        //  remove items from warehouse (Shipping -> OUT)
        foreach (var line in order.ProductInOrders ?? new List<ProductInOrder>())
        {
            _inventory.ConsumeFromShipping(line.ProductId, line.Quantity);
        }

        order.Status = OrderStatus.Shipped;
        _orderRepo.Update(order);

        return GetById(order.Id)!;
    }



    public CustomerOrder CancelOrder(Guid orderId, string performedBy)
    {
        if (string.IsNullOrWhiteSpace(performedBy)) throw new Exception("PerformedBy is required.");

        var order = _orderRepo.Get(
            selector: x => x,
            predicate: o => o.Id == orderId,
            include: q => q.Include(o => o.ProductInOrders)
        ) ?? throw new Exception("Order not found.");

        if (order.Status != OrderStatus.Ordered)
            throw new Exception("Only Ordered orders can be cancelled.");

        foreach (var line in order.ProductInOrders ?? new List<ProductInOrder>())
            _inventory.MoveFromShippingToStorage(line.ProductId, line.Quantity);

        order.Status = OrderStatus.Cancelled;
        _orderRepo.Update(order);

        return GetById(order.Id)!;
    }
}


