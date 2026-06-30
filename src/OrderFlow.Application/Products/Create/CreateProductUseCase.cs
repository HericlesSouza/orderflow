using OrderFlow.Application.Common.Results;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Products.Create;

public class CreateProductUseCase(IProductRepository productRepository)
{
    public async Task<Result<CreateProductOutput>> ExecuteAsync(CreateProductInput input)
    {
        try
        {
            var product = Product.Create(input.Name, input.Price, input.Description);

            await productRepository.AddAsync(product);

            return Result<CreateProductOutput>.Success(new CreateProductOutput(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.IsActive,
                product.CreatedAt,
                product.UpdatedAt));
        }
        catch (ArgumentException ex)
        {
            return Result<CreateProductOutput>.Failure(ex.Message);
        }
    }
}
