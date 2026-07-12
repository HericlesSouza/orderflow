using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Common.Results;
using OrderFlow.Application.Repositories;

namespace OrderFlow.Application.Orders.GetById;

public sealed class GetOrderByIdUseCase(IOrderRepository orderRepository)
{
    public async Task<Result<OrderOutput>> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(id, cancellationToken);

        if (order is null)
        {
            return Result<OrderOutput>.Failure(
                AppError.NotFound("order.not_found", "Order not found."));
        }

        return Result<OrderOutput>.Success(OrderOutput.FromEntity(order));
    }
}
