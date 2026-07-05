using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Api.Products.Create;
using OrderFlow.Application.Products.Create;
using OrderFlow.Application.Products.GetById;

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
            .Produces<ProductResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapGet("/{id:guid}", GetProductByIdAsync)
            .WithName("GetProductById")
            .WithSummary("Retrieves a product by ID.")
            .WithDescription("Retrieves a product from the catalog by its unique identifier.")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
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
            request.Price), cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(
                title: "Invalid product data.",
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        var value = result.Value!;
        var response = ProductResponse.FromOutput(value);

        return Results.Created($"/products/{value.Id}", response);
    }

    private static async Task<IResult> GetProductByIdAsync(
        [FromRoute] Guid id,
        GetProductByIdUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(id, cancellationToken);

        if (result is null)
        {
            return Results.Problem(
                title: "Product not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        var response = ProductResponse.FromOutput(result);

        return Results.Ok(response);
    }
}
