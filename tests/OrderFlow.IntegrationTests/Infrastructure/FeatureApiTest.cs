using OrderFlow.IntegrationTests.TestData;

namespace OrderFlow.IntegrationTests.Infrastructure;

public abstract class FeatureApiTest(
    OrderFlowIntegrationSuiteFixture environment,
    string feature) : IAsyncLifetime
{
    private FeatureApiContext _context = null!;

    protected HttpClient Client => _context.Client;
    protected IServiceProvider Services => _context.Services;
    protected OrderFlowTestData TestData { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _context = await environment.GetContextAsync(feature);
        await _context.ResetDatabaseAsync();
        TestData = new OrderFlowTestData(_context.Services);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
