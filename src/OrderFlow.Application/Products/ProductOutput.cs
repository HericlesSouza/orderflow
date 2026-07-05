using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Products;

public sealed record ProductOutput(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ProductOutput FromEntity(Product product)
    {
        return new ProductOutput(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.IsActive,
            product.CreatedAt,
            product.UpdatedAt);
    }
}
