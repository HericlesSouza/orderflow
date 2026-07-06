using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Api.Common.Http;
using OrderFlow.Api.Products.Create;
using OrderFlow.Api.Products.UpdatePrice;
using OrderFlow.Api.Products.UpdateStatus;
using OrderFlow.Application.Products.Create;
using OrderFlow.Application.Products.GetAll;
using OrderFlow.Application.Products.GetById;
using OrderFlow.Application.Products.UpdatePrice;
using OrderFlow.Application.Products.UpdateStatus;

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

        group.MapGet("/", GetProductsAsync)
            .WithName("GetProducts")
            .WithSummary("Lists products.")
            .WithDescription("Retrieves all products from the catalog ordered by most recent creation date.")
            .Produces<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapPatch("/{id:guid}/price", UpdateProductPriceAsync)
            .WithName("UpdateProductPrice")
            .WithSummary("Updates a product price.")
            .WithDescription("Updates the current price of a product in the catalog.")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapPatch("/{id:guid}/status", UpdateProductStatusAsync)
            .WithName("UpdateProductStatus")
            .WithSummary("Updates a product status.")
            .WithDescription("Activates or deactivates a product in the catalog.")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
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
            return result.ToProblem();
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

    private static async Task<IResult> GetProductsAsync(
        GetProductsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(cancellationToken);
        var response = result
            .Select(ProductResponse.FromOutput)
            .ToArray();

        return Results.Ok(response);
    }

    private static async Task<IResult> UpdateProductPriceAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateProductPriceRequest request,
        IValidator<UpdateProductPriceRequest> validator,
        UpdateProductPriceUseCase useCase,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await useCase.ExecuteAsync(new UpdateProductPriceInput(id, request.Price), cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblem();
        }

        return Results.Ok(ProductResponse.FromOutput(result.Value!));
    }

    private static async Task<IResult> UpdateProductStatusAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateProductStatusRequest request,
        IValidator<UpdateProductStatusRequest> validator,
        UpdateProductStatusUseCase useCase,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await useCase.ExecuteAsync(new UpdateProductStatusInput(id, request.IsActive!.Value), cancellationToken);

        if (result.IsFailure)
        {
            return result.ToProblem();
        }

        return Results.Ok(ProductResponse.FromOutput(result.Value!));
    }
}
