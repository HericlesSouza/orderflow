using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace OrderFlow.Tests.Integration.Support;

public abstract class IntegrationTestBase(OrderFlowApiFactory factory) : IClassFixture<OrderFlowApiFactory>, IAsyncLifetime
{
    protected OrderFlowApiFactory Factory { get; } = factory;

    protected HttpClient Client { get; } = factory.CreateClient();

    protected IServiceProvider Services => Factory.Services;

    protected ProductTestDataSeeder ProductSeeder { get; } = new(factory.Services);
    protected InventoryItemTestDataSeeder InventoryItemSeeder { get; } = new(factory.Services);

    public virtual Task InitializeAsync()
    {
        return Factory.ResetDatabaseAsync();
    }

    public virtual Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
