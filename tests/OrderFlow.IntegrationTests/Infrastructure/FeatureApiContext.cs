using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OrderFlow.Infrastructure.Persistence;
using Respawn;
using Respawn.Graph;

namespace OrderFlow.IntegrationTests.Infrastructure;

public sealed class FeatureApiContext : IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly OrderFlowApiFactory _factory;
    private readonly Respawner _respawner;

    private FeatureApiContext(
        string databaseName,
        string connectionString,
        OrderFlowApiFactory factory,
        HttpClient client,
        Respawner respawner)
    {
        DatabaseName = databaseName;
        _connectionString = connectionString;
        _factory = factory;
        Client = client;
        _respawner = respawner;
    }

    public string DatabaseName { get; }
    public HttpClient Client { get; }
    public IServiceProvider Services => _factory.Services;

    public static async Task<FeatureApiContext> CreateAsync(string databaseName, string connectionString)
    {
        var factory = new OrderFlowApiFactory(connectionString);
        var client = factory.CreateClient();

        try
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
                await dbContext.Database.MigrateAsync();
            }

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            var respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
                TablesToIgnore = [new Table("__EFMigrationsHistory")]
            });

            return new FeatureApiContext(databaseName, connectionString, factory, client, respawner);
        }
        catch
        {
            client.Dispose();
            await factory.DisposeAsync();
            throw;
        }
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }
}
