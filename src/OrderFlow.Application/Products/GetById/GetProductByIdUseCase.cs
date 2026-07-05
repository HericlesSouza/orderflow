using OrderFlow.Application.Products;
using OrderFlow.Application.Repositories;

namespace OrderFlow.Application.Products.GetById;

public class GetProductByIdUseCase(IProductRepository productRepository)
{
    public async Task<ProductOutput?> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken);

        return product is null
            ? null
            : ProductOutput.FromEntity(product);
    }
}
