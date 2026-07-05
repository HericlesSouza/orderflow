using OrderFlow.Application.Common.Results;
using OrderFlow.Application.Products;
using OrderFlow.Application.Repositories;

namespace OrderFlow.Application.Products.UpdateStatus;

public class UpdateProductStatusUseCase(IProductRepository productRepository)
{
    public async Task<Result<ProductOutput>> ExecuteAsync(UpdateProductStatusInput input, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(input.ProductId, cancellationToken);

        if (product is null)
            return Result<ProductOutput>.Failure("Product not found.");

        product.SetStatus(input.IsActive);

        await productRepository.UpdateAsync(product, cancellationToken);

        return Result<ProductOutput>.Success(ProductOutput.FromEntity(product));
    }
}
