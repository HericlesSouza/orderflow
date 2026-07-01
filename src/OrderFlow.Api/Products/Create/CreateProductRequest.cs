namespace OrderFlow.Api.Products.Create;

public sealed record CreateProductRequest(string Name, string Description, decimal Price);
