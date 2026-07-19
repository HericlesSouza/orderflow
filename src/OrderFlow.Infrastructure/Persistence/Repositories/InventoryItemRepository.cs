using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderFlow.Application.InventoryItems;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure.Persistence.Repositories;

public class InventoryItemRepository(OrderFlowDbContext dbContext) : IInventoryItemRepository
{
    public async Task AddAsync(InventoryItem inventoryItem, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.InventoryItems.AddAsync(inventoryItem, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new DuplicateInventoryItemException(inventoryItem.ProductId);
        }
    }

    public async Task<bool> ExistsByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        return await dbContext.InventoryItems.AnyAsync(
            inventoryItem => inventoryItem.ProductId == productId,
            cancellationToken);
    }

    public async Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        return await dbContext.InventoryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(inventoryItem => inventoryItem.ProductId == productId, cancellationToken);
    }

    public async Task<InventoryItem?> GetByProductIdForUpdateAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        return await dbContext.InventoryItems
            .FromSqlInterpolated($"""
                SELECT *
                FROM inventory_items
                WHERE product_id = {productId}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryItem>> GetByProductIdsForUpdateAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var ids = productIds.OrderBy(id => id).ToArray();

        return await dbContext.InventoryItems
            .FromSqlInterpolated($"""
                SELECT *
                FROM inventory_items
                WHERE product_id = ANY ({ids})
                ORDER BY product_id
                FOR UPDATE
                """)
            .ToArrayAsync(cancellationToken);
    }

}
