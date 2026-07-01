using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Api.Products.Create;
using OrderFlow.Application.Products.Create;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Api.Products;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/products")
            .WithTags("Products");

        group.MapPost("/", CreateProductAsync)
            .WithName("CreateProduct")
            .WithSummary("Creates a product.")
            .WithDescription("Creates a new product in the catalog.")
            .Produces<CreateProductResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> CreateProductAsync(
        [FromBody] CreateProductRequest request,
        IValidator<CreateProductRequest> validator,
        CreateProductUseCase useCase,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await useCase.ExecuteAsync(new CreateProductInput(
            request.Name,
            request.Description,
            request.Price));

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Invalid product data.",
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        var value = result.Value!;
        var response = new CreateProductResponse(
            value.Id,
            value.Name,
            value.Description,
            value.Price,
            value.IsActive,
            value.CreatedAt,
            value.UpdatedAt);

        return Results.Created($"/products/{value.Id}", response);
    }
}
