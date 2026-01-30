using System;
using System.Collections.Generic;
using System.Text;
using Warehouse.Domain.Domain;
using Warehouse.Domain.Domain.Enums;

namespace Warehouse.Repository.Interface;

public interface IStockBalanceRepository
{
    Task<StockBalance> GetOrCreateAsync(Guid productId, LocationType locationType);
    Task<List<StockBalance>> GetForProductAsync(Guid productId);
    Task SaveAsync();
}

