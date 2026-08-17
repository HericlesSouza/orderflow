using System.Collections.Concurrent;
using Npgsql;
using Testcontainers.PostgreSql;

namespace OrderFlow.IntegrationTests.Infrastructure;

public sealed class OrderFlowIntegrationSuiteFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("orderflow_tests")
        .WithUsername("orderflow")
        .WithPassword("orderflow_tests")
        .Build();

    private readonly ConcurrentDictionary<string, Lazy<Task<FeatureApiContext>>> _contexts = new();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
    }

    public Task<FeatureApiContext> GetContextAsync(string feature)
    {
        var context = _contexts.GetOrAdd(
            feature,
            key => new Lazy<Task<FeatureApiContext>>(
                () => CreateContextAsync(key),
                LazyThreadSafetyMode.ExecutionAndPublication));

        return context.Value;
    }

    public async ValueTask DisposeAsync()
    {
        var failures = new List<Exception>();

        foreach (var lazyContext in _contexts.Values.Where(value => value.IsValueCreated))
        {
            FeatureApiContext? context = null;

            try
            {
                context = await lazyContext.Value;
                await context.DisposeAsync();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }

            if (context is not null)
            {
                try
                {
                    await DropDatabaseAsync(context.DatabaseName);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }

        try
        {
            await _container.DisposeAsync();
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        if (failures.Count > 0)
        {
            throw new AggregateException("Failed to dispose the integration test environment.", failures);
        }
    }

    private async Task<FeatureApiContext> CreateContextAsync(string feature)
    {
        var databaseName = $"orderflow_{feature.ToLowerInvariant()}_{Guid.NewGuid():N}";
        var adminConnectionString = _container.GetConnectionString();

        await using (var connection = new NpgsqlConnection(adminConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await command.ExecuteNonQueryAsync();
        }

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = databaseName
        };

        try
        {
            return await FeatureApiContext.CreateAsync(
                databaseName,
                connectionStringBuilder.ConnectionString);
        }
        catch
        {
            await DropDatabaseAsync(databaseName);
            throw;
        }
    }
    private async Task DropDatabaseAsync(string databaseName)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)";
        await command.ExecuteNonQueryAsync();
    }
}
