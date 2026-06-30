namespace OrderFlow.Application.Products.Create;

public sealed record CreateProductOutput(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
