using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Common.Results;
using OrderFlow.Application.Common.Transactions;
using OrderFlow.Application.Orders;
using OrderFlow.Application.Repositories;

namespace OrderFlow.Application.Payments.Approve;

public sealed class ApprovePaymentUseCase(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
{
    public Task<Result<OrderOutput>> ExecuteAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteInTransactionAsync(
            transactionCancellationToken => ApproveAsync(orderId, transactionCancellationToken),
            cancellationToken);
    }

    private async Task<Result<OrderOutput>> ApproveAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(orderId, cancellationToken);

        if (order is null)
        {
            return Result<OrderOutput>.Failure(
                AppError.NotFound("order.not_found", "Order not found."));
        }

        try
        {
            order.ConfirmPayment();
        }
        catch (InvalidOperationException ex)
        {
            return Result<OrderOutput>.Failure(
                AppError.Conflict("payment.order_not_pending", ex.Message));
        }

        return Result<OrderOutput>.Success(OrderOutput.FromEntity(order));
    }
}
