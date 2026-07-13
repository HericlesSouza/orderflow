using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Repositories;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Order?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);
}
