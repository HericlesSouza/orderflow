using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.IntegrationTests.TestData;

public sealed class OrderFlowTestData(IServiceProvider serviceProvider)
{
    public ProductTestDataBuilder Product()
    {
        return new ProductTestDataBuilder(serviceProvider);
    }

    public InventoryItemTestDataBuilder InventoryItem()
    {
        return new InventoryItemTestDataBuilder(serviceProvider);
    }

    public async Task UpdateProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        dbContext.Products.Update(product);
        await dbContext.SaveChangesAsync(cancellationToken);
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
