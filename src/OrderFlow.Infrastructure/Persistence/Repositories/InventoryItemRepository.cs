using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderFlow.Application.Repositories;
using OrderFlow.Application.InventoryItems;
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
}
