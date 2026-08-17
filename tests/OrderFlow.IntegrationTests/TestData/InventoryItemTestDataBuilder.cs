using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.IntegrationTests.TestData;

public sealed class InventoryItemTestDataBuilder
{
    private readonly IServiceProvider _serviceProvider;
    private Guid _productId;
    private int _quantity = 10;

    public InventoryItemTestDataBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public InventoryItemTestDataBuilder ForProduct(Guid productId)
    {
        _productId = productId;
        return this;
    }

    public InventoryItemTestDataBuilder WithQuantity(int quantity)
    {
        _quantity = quantity;
        return this;
    }

    public async Task<InventoryItem> PersistAsync(CancellationToken cancellationToken = default)
    {
        var inventoryItem = InventoryItem.Create(_productId, _quantity);

        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        await dbContext.InventoryItems.AddAsync(inventoryItem, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return inventoryItem;
    }
}
