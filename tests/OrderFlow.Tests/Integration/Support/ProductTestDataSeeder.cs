using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Tests.Integration.Support;

public sealed class ProductTestDataSeeder(IServiceProvider serviceProvider)
{
    public async Task<Product> CreateProductAsync(
        string name = "Notebook",
        string description = "Gaming notebook",
        decimal price = 3500.00m,
        bool isActive = true)
    {
        var product = Product.Create(name, price, description);

        if (!isActive)
        {
            product.Deactivate();
        }

        await SaveProductAsync(product);

        return product;
    }

    public async Task SaveProductAsync(Product product)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();

        await dbContext.Products.AddAsync(product);
        await dbContext.SaveChangesAsync();
    }

    public async Task SaveProductsAsync(IEnumerable<Product> products)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();

        await dbContext.Products.AddRangeAsync(products);
        await dbContext.SaveChangesAsync();
    }

    public async Task<Product?> GetProductAsync(Guid productId)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();

        return await dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(product => product.Id == productId);
    }
}
