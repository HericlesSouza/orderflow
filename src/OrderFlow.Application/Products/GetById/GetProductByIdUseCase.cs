using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Common.Results;
using OrderFlow.Application.Products;
using OrderFlow.Application.Repositories;

namespace OrderFlow.Application.Products.GetById;

public class GetProductByIdUseCase(IProductRepository productRepository)
{
    public async Task<Result<ProductOutput>> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken);

        if (product is null)
            return Result<ProductOutput>.Failure(AppError.NotFound("product.not_found", "Product not found."));

        return Result<ProductOutput>.Success(ProductOutput.FromEntity(product));
    }
}
