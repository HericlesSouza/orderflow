using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Api.InventoryItems;
using OrderFlow.Api.InventoryItems.Increase;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.InventoryItems.Increase;

[Collection<InventoryApiCollection>]
[Trait("Layer", "Api")]
[Trait("Feature", "Inventory")]
public sealed class IncreaseInventoryItemStockEndpointTests(OrderFlowIntegrationSuiteFixture environment) : FeatureApiTest(environment, IntegrationFeatures.Inventory)
{
    [Fact]
    public async Task Should_Return_Updated_Inventory_Item_When_Request_Is_Valid()
    {
        var product = await TestData.Product().PersistAsync(TestContext.Current.CancellationToken);
        var inventoryItem = await TestData.InventoryItem().ForProduct(product.Id).WithQuantity(10).PersistAsync(TestContext.Current.CancellationToken);

        var response = await Client.PostAsJsonAsync($"/inventory/items/{product.Id}/increase", new IncreaseInventoryItemStockRequest(5), cancellationToken: TestContext.Current.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<InventoryItemResponse>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(inventoryItem.Id, body.Id);
        Assert.Equal(product.Id, body.ProductId);
        Assert.Equal(15, body.Quantity);
        Assert.Equal(0, body.ReservedQuantity);
        Assert.Equal(15, body.AvailableQuantity);
        Assert.Equal(inventoryItem.CreatedAt, body.CreatedAt, TimeSpan.FromMilliseconds(1));
        Assert.True(body.UpdatedAt >= inventoryItem.UpdatedAt);
    }

    [Fact]
    public async Task Should_Persist_Increased_Inventory_Stock_When_Request_Is_Valid()
    {
        var product = await TestData.Product().PersistAsync(TestContext.Current.CancellationToken);
        await TestData.InventoryItem().ForProduct(product.Id).WithQuantity(10).PersistAsync(TestContext.Current.CancellationToken);

        var response = await Client.PostAsJsonAsync($"/inventory/items/{product.Id}/increase", new IncreaseInventoryItemStockRequest(5), cancellationToken: TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var inventoryItem = await dbContext.InventoryItems.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(15, inventoryItem.Quantity);
        Assert.Equal(0, inventoryItem.ReservedQuantity);
        Assert.Equal(15, inventoryItem.AvailableQuantity);
    }

    [Fact]
    public async Task Should_Preserve_Both_Increases_When_Requests_Are_Concurrent()
    {
        var product = await TestData.Product().PersistAsync(TestContext.Current.CancellationToken);
        await TestData.InventoryItem().ForProduct(product.Id).WithQuantity(10).PersistAsync(TestContext.Current.CancellationToken);

        var responses = await Task.WhenAll(
            Client.PostAsJsonAsync(
                $"/inventory/items/{product.Id}/increase",
                new IncreaseInventoryItemStockRequest(5), TestContext.Current.CancellationToken),
            Client.PostAsJsonAsync(
                $"/inventory/items/{product.Id}/increase",
                new IncreaseInventoryItemStockRequest(5), TestContext.Current.CancellationToken));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var inventoryItem = await dbContext.InventoryItems.AsNoTracking().SingleAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(20, inventoryItem.Quantity);
        Assert.Equal(0, inventoryItem.ReservedQuantity);
        Assert.Equal(20, inventoryItem.AvailableQuantity);
    }

    [Fact]
    public async Task Should_Preserve_Concurrent_Reservation_When_Increasing_Stock()
    {
        var product = await TestData.Product().PersistAsync(TestContext.Current.CancellationToken);
        await TestData.InventoryItem().ForProduct(product.Id).WithQuantity(10).PersistAsync(TestContext.Current.CancellationToken);
        var warmupResponse = await Client.GetAsync($"/inventory/items/{product.Id}", TestContext.Current.CancellationToken);
        warmupResponse.EnsureSuccessStatusCode();

        await using var reservationScope = Services.CreateAsyncScope();
        var reservationDbContext = reservationScope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        await using var reservationTransaction = await reservationDbContext.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        var lockedInventoryItem = await reservationDbContext.InventoryItems
            .FromSqlInterpolated($"""
                SELECT *
                FROM inventory_items
                WHERE product_id = {product.Id}
                FOR UPDATE
                """)
            .SingleAsync(cancellationToken: TestContext.Current.CancellationToken);

        var increaseTask = Client.PostAsJsonAsync($"/inventory/items/{product.Id}/increase", new IncreaseInventoryItemStockRequest(5), cancellationToken: TestContext.Current.CancellationToken);

        await Task.Delay(200, TestContext.Current.CancellationToken);
        var increaseWaitedForReservation = !increaseTask.IsCompleted;

        lockedInventoryItem.Reserve(3);
        await reservationDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await reservationTransaction.CommitAsync(TestContext.Current.CancellationToken);

        var response = await increaseTask.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        Assert.True(increaseWaitedForReservation);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var verificationScope = Services.CreateAsyncScope();
        var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var inventoryItem = await verificationDbContext.InventoryItems.AsNoTracking().SingleAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(15, inventoryItem.Quantity);
        Assert.Equal(3, inventoryItem.ReservedQuantity);
        Assert.Equal(12, inventoryItem.AvailableQuantity);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Quantity_Is_Invalid()
    {
        var product = await TestData.Product().PersistAsync(TestContext.Current.CancellationToken);
        await TestData.InventoryItem().ForProduct(product.Id).WithQuantity(10).PersistAsync(TestContext.Current.CancellationToken);

        var response = await Client.PostAsJsonAsync($"/inventory/items/{product.Id}/increase", new IncreaseInventoryItemStockRequest(0), cancellationToken: TestContext.Current.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains(nameof(IncreaseInventoryItemStockRequest.Quantity), body.Errors.Keys);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Inventory_Item_Does_Not_Exist()
    {
        var product = await TestData.Product().PersistAsync(TestContext.Current.CancellationToken);

        var response = await Client.PostAsJsonAsync($"/inventory/items/{product.Id}/increase", new IncreaseInventoryItemStockRequest(5), cancellationToken: TestContext.Current.CancellationToken);

        var body = await response.ReadProblemDetailsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Resource not found.", body.Title);
        Assert.Equal("No inventory item found for the specified product.", body.Detail);
    }
}
