using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Warehouse.Domain.Domain;
using Warehouse.Domain.Domain.Enums;
using Warehouse.Repository.Interface;

namespace Warehouse.Repository.Implementation;

public class StockBalanceRepository : IStockBalanceRepository
{
    private readonly ApplicationDbContext _context;

    public StockBalanceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<StockBalance>> GetForProductAsync(Guid productId)
    {
        return await _context.StockBalances
            .Where(x => x.ProductId == productId)
            .ToListAsync();
    }

    public async Task<StockBalance> GetOrCreateAsync(Guid productId, LocationType locationType)
    {
        var sb = await _context.StockBalances
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.LocationType == locationType);

        if (sb != null) return sb;

        sb = new StockBalance
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            LocationType = locationType,
            Quantity = 0
        };

        _context.StockBalances.Add(sb);
        return sb;
    }

    public Task SaveAsync() => _context.SaveChangesAsync();
}
