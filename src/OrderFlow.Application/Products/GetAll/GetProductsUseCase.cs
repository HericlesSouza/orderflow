using OrderFlow.Application.Products;
using OrderFlow.Application.Repositories;

namespace OrderFlow.Application.Products.GetAll;

public class GetProductsUseCase(IProductRepository productRepository)
{
    public async Task<IReadOnlyList<ProductOutput>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var products = await productRepository.ListAsync(cancellationToken);

        return [.. products.Select(ProductOutput.FromEntity)];
    }
}
