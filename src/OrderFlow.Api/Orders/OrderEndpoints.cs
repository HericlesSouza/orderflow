using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Api.Common.Http;
using OrderFlow.Api.Orders.Create;
using OrderFlow.Application.Orders.Cancel;
using OrderFlow.Application.Orders.Create;
using OrderFlow.Application.Orders.Dispatch;
using OrderFlow.Application.Orders.GetById;

namespace OrderFlow.Api.Orders;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/orders")
            .WithTags("Orders");

        group.MapPost("/", CreateOrderAsync)
            .WithName("CreateOrder")
            .WithSummary("Creates an order.")
            .WithDescription("Creates an order and reserves inventory atomically.")
            .Produces<OrderResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapGet("/{id:guid}", GetOrderByIdAsync)
            .WithName("GetOrderById")
            .WithSummary("Retrieves an order by ID.")
            .WithDescription("Retrieves an order and all its items by its unique identifier.")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapPost("/{id:guid}/cancel", CancelOrderAsync)
            .WithName("CancelOrder")
            .WithSummary("Cancels an order.")
            .WithDescription("Cancels a pending payment order and releases its reserved inventory atomically.")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapPost("/{id:guid}/dispatch", DispatchOrderAsync)
            .WithName("DispatchOrder")
            .WithSummary("Dispatches a confirmed order.")
            .WithDescription("Dispatches all order items and atomically deducts their reserved inventory.")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> CreateOrderAsync(
        [FromBody] CreateOrderRequest request,
        IValidator<CreateOrderRequest> validator,
        CreateOrderUseCase useCase,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var input = new CreateOrderInput(
            request.Items
                .Select(item => new CreateOrderItemInput(item.ProductId, item.Quantity))
                .ToArray());
        var result = await useCase.ExecuteAsync(input, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblem();
        }

        var response = OrderResponse.FromOutput(result.Value!);

        return Results.Created($"/orders/{response.Id}", response);
    }

    private static async Task<IResult> GetOrderByIdAsync(
        [FromRoute] Guid id,
        GetOrderByIdUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblem();
        }

        return Results.Ok(OrderResponse.FromOutput(result.Value!));
    }

    private static async Task<IResult> CancelOrderAsync(
        [FromRoute] Guid id,
        CancelOrderUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblem();
        }

        return Results.Ok(OrderResponse.FromOutput(result.Value!));
    }

    private static async Task<IResult> DispatchOrderAsync(
        [FromRoute] Guid id,
        DispatchOrderUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblem();
        }

        return Results.Ok(OrderResponse.FromOutput(result.Value!));
    }
}
