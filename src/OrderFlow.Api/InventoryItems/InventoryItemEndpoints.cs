using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Api.Common.Http;
using OrderFlow.Api.InventoryItems.Create;
using OrderFlow.Application.InventoryItems.Create;
using OrderFlow.Application.InventoryItems.GetByProductId;

namespace OrderFlow.Api.InventoryItems;

public static class InventoryItemEndpoints
{
    public static IEndpointRouteBuilder MapInventoryItemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/inventory/items")
            .WithTags("Inventory Items");

        group.MapPost("/", CreateInventoryItemAsync)
            .WithName("CreateInventoryItem")
            .WithSummary("Creates an inventory item.")
            .WithDescription("Creates a new inventory item for a product.")
            .Produces<InventoryItemResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapGet("/{productId:guid}", GetInventoryByProductIdAsync)
            .WithName("GetInventoryByProductId")
            .WithSummary("Gets an inventory item by product ID.")
            .WithDescription("Retrieves the inventory item associated with the specified product ID.")
            .Produces<InventoryItemResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> CreateInventoryItemAsync(
        [FromBody] CreateInventoryItemRequest request,
        IValidator<CreateInventoryItemRequest> validator,
        CreateInventoryItemUseCase useCase,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var input = new CreateInventoryItemInput(request.ProductId, request.Quantity);
        var result = await useCase.ExecuteAsync(input, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblem();
        }

        var response = InventoryItemResponse.FromOutput(result.Value!);

        return Results.Created($"/inventory/items/{response.ProductId}", response);
    }

    private static async Task<IResult> GetInventoryByProductIdAsync(
        [FromRoute] Guid productId,
        GetInventoryItemByProductIdUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(productId, cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblem();
        }

        var response = InventoryItemResponse.FromOutput(result.Value!);

        return Results.Ok(response);
    }
}
