using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Repositories;

public interface IProductRepository
{
    Task AddAsync(Product product);
}
