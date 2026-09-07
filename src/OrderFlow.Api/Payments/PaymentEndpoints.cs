using Microsoft.AspNetCore.Mvc;
using OrderFlow.Api.Common.Http;
using OrderFlow.Api.Orders;
using OrderFlow.Application.Payments.Approve;
using OrderFlow.Application.Payments.Reject;

namespace OrderFlow.Api.Payments;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/payments").WithTags("Payments");

        group.MapPost("/{orderId:guid}/approve", ApprovePaymentAsync)
            .WithName("ApprovePayment")
            .WithSummary("Approves a simulated payment.")
            .WithDescription("Confirms a pending payment order while keeping inventory reserved for future dispatch.")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapPost("/{orderId:guid}/reject", RejectPaymentAsync)
            .WithName("RejectPayment")
            .WithSummary("Rejects a simulated payment.")
            .WithDescription("Rejects a pending payment order and releases its reserved inventory atomically.")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> ApprovePaymentAsync(
        [FromRoute] Guid orderId,
        ApprovePaymentUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(orderId, cancellationToken);

        if (result.IsFailure)
            return result.ToProblem();

        return Results.Ok(OrderResponse.FromOutput(result.Value!));
    }

    private static async Task<IResult> RejectPaymentAsync(
        [FromRoute] Guid orderId,
        RejectPaymentUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(orderId, cancellationToken);

        if (result.IsFailure)
            return result.ToProblem();

        return Results.Ok(OrderResponse.FromOutput(result.Value!));
    }
}
