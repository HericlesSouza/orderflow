using OrderFlow.Application.Common.Results;
using OrderFlow.Application.Products;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Products.Create;

public class CreateProductUseCase(IProductRepository productRepository)
{
    public async Task<Result<ProductOutput>> ExecuteAsync(CreateProductInput input, CancellationToken cancellationToken)
    {
        try
        {
            var product = Product.Create(input.Name, input.Price, input.Description);

            await productRepository.AddAsync(product, cancellationToken);

            return Result<ProductOutput>.Success(ProductOutput.FromEntity(product));
        }
        catch (ArgumentException ex)
        {
            return Result<ProductOutput>.Failure(ex.Message);
        }
    }
}
