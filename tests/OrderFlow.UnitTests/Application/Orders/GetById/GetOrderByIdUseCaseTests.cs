using Moq;
using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Orders.GetById;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;
using OrderFlow.UnitTests.Common.Builders;

namespace OrderFlow.UnitTests.Application.Orders.GetById;

[Trait("Layer", "Application")]
[Trait("Feature", "Orders")]
public sealed class GetOrderByIdUseCaseTests
{
    [Fact]
    public async Task Should_Return_Order_When_Order_Exists()
    {
        var order = new OrderBuilder().WithItems(
            new OrderItemBuilder().WithQuantity(2).WithUnitPrice(100.00m).Build()).Build();
        var repository = new Mock<IOrderRepository>();
        repository
            .Setup(value => value.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var useCase = new GetOrderByIdUseCase(repository.Object);

        var result = await useCase.ExecuteAsync(order.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(order.Id, result.Value!.Id);
        Assert.Equal(200.00m, result.Value.TotalAmount);
        Assert.Single(result.Value.Items);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Order_Does_Not_Exist()
    {
        var repository = new Mock<IOrderRepository>();
        repository
            .Setup(value => value.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        var useCase = new GetOrderByIdUseCase(repository.Object);

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("order.not_found", result.Error!.Code);
        Assert.Equal(AppErrorType.NotFound, result.Error.Type);
    }
}
