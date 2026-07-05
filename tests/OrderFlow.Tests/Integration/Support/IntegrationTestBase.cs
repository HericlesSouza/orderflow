using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace OrderFlow.Tests.Integration.Support;

public abstract class IntegrationTestBase(ProductApiFactory factory) : IClassFixture<ProductApiFactory>, IAsyncLifetime
{
    protected ProductApiFactory Factory { get; } = factory;

    protected HttpClient Client { get; } = factory.CreateClient();

    protected IServiceProvider Services => Factory.Services;

    protected ProductTestDataSeeder ProductSeeder { get; } = new(factory.Services);

    public virtual Task InitializeAsync()
    {
        return Factory.ResetDatabaseAsync();
    }

    public virtual Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
