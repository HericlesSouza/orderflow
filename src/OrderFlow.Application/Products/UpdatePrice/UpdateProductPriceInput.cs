namespace OrderFlow.Application.Products.UpdatePrice;

public sealed record UpdateProductPriceInput(Guid ProductId, decimal Price);
