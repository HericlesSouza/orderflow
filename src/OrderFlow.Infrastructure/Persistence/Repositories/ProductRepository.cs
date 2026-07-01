using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Repositories;

public class ProductRepository(OrderFlowDbContext dbContext) : IProductRepository
{
    public async Task AddAsync(Product product)
    {
        await dbContext.Products.AddAsync(product);
        await dbContext.SaveChangesAsync();
    }
}
