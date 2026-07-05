using OrderFlow.Application.Products;

namespace OrderFlow.Api.Products;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ProductResponse FromOutput(ProductOutput output)
    {
        return new ProductResponse(
            output.Id,
            output.Name,
            output.Description,
            output.Price,
            output.IsActive,
            output.CreatedAt,
            output.UpdatedAt);
    }
}
