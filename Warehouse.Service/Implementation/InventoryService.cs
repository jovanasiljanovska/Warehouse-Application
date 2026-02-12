using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

using Warehouse.Domain.Domain;
using Warehouse.Domain.Domain.Enums;
using Warehouse.Repository.Interface;
using Warehouse.Service.Interface;

namespace Warehouse.Service.Implementation;

public class InventoryService : IInventoryService
{
    private readonly IRepository<StockBalance> _stockRepo;

    public InventoryService(IRepository<StockBalance> stockRepo)
    {
        _stockRepo = stockRepo;
    }

    private StockBalance GetOrCreate(Guid productId, LocationType type)
    {
        var sb = _stockRepo.Get(
            selector: x => x,
            predicate: x => x.ProductId == productId && x.LocationType == type
        );

        if (sb != null) return sb;

        return _stockRepo.Insert(new StockBalance
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            LocationType = type,
            Quantity = 0
        });
    }

    public void SetInitialStock(Guid productId, int quantity, LocationType locationType)
    {
        if (quantity < 0) throw new Exception("Quantity cannot be negative.");

        var existing = _stockRepo.Get(
            selector: x => x,
            predicate: s => s.ProductId == productId && s.LocationType == locationType);

        if (existing == null)
        {
            _stockRepo.Insert(new StockBalance
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                LocationType = locationType,
                Quantity = quantity
            });
        }
        else
        {
            existing.Quantity += quantity;
            _stockRepo.Update(existing);
        }
    }


    public void MoveToShipping(Guid productId, int quantity)
    {
        if (quantity <= 0) throw new Exception("Quantity must be > 0.");

        var shelves = GetOrCreate(productId, LocationType.Shelves);
        var freezer = GetOrCreate(productId, LocationType.Freezer);
        var shipping = GetOrCreate(productId, LocationType.Shipping);

        var available = shelves.Quantity + freezer.Quantity;
        if (available < quantity) throw new Exception("Not enough stock in Shelves + Freezer.");

        var need = quantity;

        // Shelves first
        var takeShelves = Math.Min(shelves.Quantity, need);
        shelves.Quantity -= takeShelves;
        shipping.Quantity += takeShelves;
        need -= takeShelves;

        // then freezer
        if (need > 0)
        {
            var takeFreezer = Math.Min(freezer.Quantity, need);
            freezer.Quantity -= takeFreezer;
            shipping.Quantity += takeFreezer;
            need -= takeFreezer;
        }

        _stockRepo.Update(shelves);
        _stockRepo.Update(freezer);
        _stockRepo.Update(shipping);
    }

    public void MoveFromShippingToStorage(Guid productId, int quantity)
    {
        if (quantity <= 0) throw new Exception("Quantity must be > 0.");

        var shipping = GetOrCreate(productId, LocationType.Shipping);
        if (shipping.Quantity < quantity) throw new Exception("Not enough stock in Shipping.");

        var shelves = GetOrCreate(productId, LocationType.Shelves);

        shipping.Quantity -= quantity;
        shelves.Quantity += quantity;

        _stockRepo.Update(shipping);
        _stockRepo.Update(shelves);
    }

    public void AddToReceiving(Guid productId, int quantity)
    {
        if (quantity <= 0) throw new Exception("Quantity must be > 0.");

        var receiving = GetOrCreate(productId, LocationType.Receiving);
        receiving.Quantity += quantity;

        _stockRepo.Update(receiving);
    }

    public void PutAway(Guid productId, int quantity, LocationType target)
    {
        if (quantity <= 0) throw new Exception("Quantity must be > 0.");
        if (target != LocationType.Shelves && target != LocationType.Freezer)
            throw new Exception("PutAway target must be Shelves or Freezer.");

        var receiving = GetOrCreate(productId, LocationType.Receiving);
        if (receiving.Quantity < quantity) throw new Exception("Not enough stock in Receiving.");

        var dest = GetOrCreate(productId, target);

        receiving.Quantity -= quantity;
        dest.Quantity += quantity;

        _stockRepo.Update(receiving);
        _stockRepo.Update(dest);
    }

    public void ConsumeFromShipping(Guid productId, int quantity)
    {
        if (quantity <= 0) throw new Exception("Quantity must be > 0.");

        var shipping = _stockRepo.Get(
            selector: x => x,
            predicate: sb => sb.ProductId == productId && sb.LocationType == LocationType.Shipping
        ) ?? throw new Exception("No stock in Shipping for this product.");

        if (shipping.Quantity < quantity)
            throw new Exception($"Not enough quantity in Shipping. Available: {shipping.Quantity}");

        shipping.Quantity -= quantity;

        if (shipping.Quantity == 0)
            _stockRepo.Delete(shipping);
        else
            _stockRepo.Update(shipping);
    }

}

