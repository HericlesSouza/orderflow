using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Application.Common.Outbox;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.IntegrationTests.Outbox;

internal sealed class OutboxMessageRepositoryFixture : IAsyncDisposable
{
    private readonly AsyncServiceScope _scope;
    private readonly OrderFlowDbContext _dbContext;

    public OutboxMessageRepositoryFixture(IServiceProvider services)
    {
        _scope = services.CreateAsyncScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        Repository = _scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
    }

    public IOutboxMessageRepository Repository { get; }

    public async Task InsertAsync(
        OutboxMessageRow message,
        CancellationToken cancellationToken)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO outbox_messages (
                id,
                event_type,
                payload,
                created_at,
                processed_at,
                attempt_count,
                last_attempted_at,
                last_error)
            VALUES (
                {message.Id},
                {message.EventType},
                CAST({message.Payload} AS jsonb),
                {message.CreatedAt},
                {message.ProcessedAt},
                {message.AttemptCount},
                {message.LastAttemptedAt},
                {message.LastError})
            """, cancellationToken);
    }

    public async Task<OutboxMessageRow> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _dbContext.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = """
                SELECT
                    id,
                    event_type,
                    payload::text,
                    created_at,
                    processed_at,
                    attempt_count,
                    last_attempted_at,
                    last_error
                FROM outbox_messages
                WHERE id = @id
                """;

            var idParameter = command.CreateParameter();
            idParameter.ParameterName = "id";
            idParameter.DbType = DbType.Guid;
            idParameter.Value = id;
            command.Parameters.Add(idParameter);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            Assert.True(await reader.ReadAsync(cancellationToken));

            return new OutboxMessageRow(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDateTime(3),
                reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                reader.GetInt32(5),
                reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                reader.IsDBNull(7) ? null : reader.GetString(7));
        }
        finally
        {
            await _dbContext.Database.CloseConnectionAsync();
        }
    }

    public ValueTask DisposeAsync()
    {
        return _scope.DisposeAsync();
    }
}

internal sealed record OutboxMessageRow(
    Guid Id,
    string EventType,
    string Payload,
    DateTime CreatedAt,
    DateTime? ProcessedAt = null,
    int AttemptCount = 0,
    DateTime? LastAttemptedAt = null,
    string? LastError = null);
