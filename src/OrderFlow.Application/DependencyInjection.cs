using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Application.Products.Create;
using OrderFlow.Application.Products.GetById;

namespace OrderFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateProductUseCase>();
        services.AddScoped<GetProductByIdUseCase>();

        return services;
    }
}
