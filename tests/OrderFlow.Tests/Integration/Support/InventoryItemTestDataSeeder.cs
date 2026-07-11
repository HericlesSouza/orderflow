using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Tests.Integration.Support;

public sealed class InventoryItemTestDataSeeder(IServiceProvider serviceProvider)
{
    public async Task<InventoryItem> CreateInventoryItemAsync(Guid productId, int quantity = 10)
    {
        var inventoryItem = InventoryItem.Create(productId, quantity);

        await SaveInventoryItemAsync(inventoryItem);

        return inventoryItem;
    }

    public async Task SaveInventoryItemAsync(InventoryItem inventoryItem)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();

        await dbContext.InventoryItems.AddAsync(inventoryItem);
        await dbContext.SaveChangesAsync();
    }

    public async Task<InventoryItem?> GetInventoryItemByProductIdAsync(Guid productId)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();

        return await dbContext.InventoryItems
            .AsNoTracking()
            .SingleOrDefaultAsync(inventoryItem => inventoryItem.ProductId == productId);
    }
}
