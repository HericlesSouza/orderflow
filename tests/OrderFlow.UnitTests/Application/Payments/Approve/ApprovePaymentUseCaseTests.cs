using Moq;
using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Payments.Approve;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.UnitTests.Common.Builders;
using OrderFlow.UnitTests.Common.Fakes;

namespace OrderFlow.UnitTests.Application.Payments.Approve;

[Trait("Layer", "Application")]
[Trait("Feature", "Payments")]
public sealed class ApprovePaymentUseCaseTests
{
    [Fact]
    public async Task Should_Confirm_Order_And_Preserve_Order_Data_When_Pending()
    {
        var order = new OrderBuilder().Build();
        var repository = CreateRepository(order);
        var unitOfWork = new ImmediateUnitOfWork();
        var useCase = new ApprovePaymentUseCase(repository.Object, unitOfWork);

        var result = await useCase.ExecuteAsync(order.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Confirmed", result.Value!.Status);
        Assert.Equal(order.Id, result.Value.Id);
        Assert.Equal(order.TotalAmount, result.Value.TotalAmount);
        Assert.Equal(order.CreatedAt, result.Value.CreatedAt);
        Assert.Null(result.Value.CancelledAt);
        Assert.Equal(order.OrderItems.Count, result.Value.Items.Count);
        Assert.Equal(1, unitOfWork.ExecutionCount);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Order_Does_Not_Exist()
    {
        var repository = CreateRepository(null);
        var useCase = new ApprovePaymentUseCase(repository.Object, new ImmediateUnitOfWork());

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AppErrorType.NotFound, result.Error!.Type);
        Assert.Equal("order.not_found", result.Error.Code);
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.PaymentRejected)]
    [InlineData(OrderStatus.Dispatched)]
    public async Task Should_Return_Conflict_Without_Changing_A_Non_Pending_Order(OrderStatus status)
    {
        var order = new OrderBuilder().Build();
        switch (status)
        {
            case OrderStatus.Confirmed: order.ConfirmPayment(); break;
            case OrderStatus.Cancelled: order.Cancel(); break;
            case OrderStatus.PaymentRejected: order.RejectPayment(); break;
            case OrderStatus.Dispatched: order.ConfirmPayment(); order.Dispatch(); break;
        }
        var repository = CreateRepository(order);
        var useCase = new ApprovePaymentUseCase(repository.Object, new ImmediateUnitOfWork());

        var result = await useCase.ExecuteAsync(order.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AppErrorType.Conflict, result.Error!.Type);
        Assert.Equal("payment.order_not_pending", result.Error.Code);
        Assert.Equal(status, order.Status);
    }

    [Fact]
    public async Task Should_Propagate_Repository_Failure_Instead_Of_Returning_State_Conflict()
    {
        var repository = new Mock<IOrderRepository>();
        var exception = new InvalidOperationException("Persistence failure");
        repository.Setup(value => value.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
        var useCase = new ApprovePaymentUseCase(repository.Object, new ImmediateUnitOfWork());

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Same(exception, actual);
    }

    private static Mock<IOrderRepository> CreateRepository(Order? order)
    {
        var repository = new Mock<IOrderRepository>();
        repository.Setup(value => value.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        return repository;
    }
}
