using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Application.Common.Outbox;
using OrderFlow.Application.Common.Transactions;
using OrderFlow.Application.Repositories;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.Infrastructure.Persistence.Outbox;
using OrderFlow.Infrastructure.Persistence.Repositories;

namespace OrderFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string environmentName)
    {
        var connectionString = configuration.GetConnectionString("OrderFlowDatabase");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<OrderFlowDbContext>(options =>
                options.UseNpgsql(connectionString));
        }
        else if (!string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Connection string 'OrderFlowDatabase' was not found.");
        }

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOutboxMessageWriter, OutboxMessageWriter>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
