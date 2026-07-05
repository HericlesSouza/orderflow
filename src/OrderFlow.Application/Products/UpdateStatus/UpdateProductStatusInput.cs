namespace OrderFlow.Application.Products.UpdateStatus;

public sealed record UpdateProductStatusInput(Guid ProductId, bool IsActive);
