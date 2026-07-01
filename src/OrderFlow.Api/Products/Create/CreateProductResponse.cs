namespace OrderFlow.Api.Products.Create;

public sealed record CreateProductResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
