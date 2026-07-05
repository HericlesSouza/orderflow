using OrderFlow.Application.Common.Results;
using OrderFlow.Application.Products;
using OrderFlow.Application.Repositories;

namespace OrderFlow.Application.Products.UpdatePrice;

public class UpdateProductPriceUseCase(IProductRepository productRepository)
{
    public async Task<Result<ProductOutput>> ExecuteAsync(UpdateProductPriceInput input, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(input.ProductId, cancellationToken);

        if (product is null)
            return Result<ProductOutput>.Failure("Product not found.");

        try
        {
            product.ChangePrice(input.Price);

            await productRepository.UpdateAsync(product, cancellationToken);

            return Result<ProductOutput>.Success(ProductOutput.FromEntity(product));
        }
        catch (ArgumentException ex)
        {
            return Result<ProductOutput>.Failure(ex.Message);
        }
    }
}
