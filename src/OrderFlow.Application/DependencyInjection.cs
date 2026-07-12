using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Application.InventoryItems.Create;
using OrderFlow.Application.InventoryItems.GetByProductId;
using OrderFlow.Application.InventoryItems.Increase;
using OrderFlow.Application.Orders.Create;
using OrderFlow.Application.Orders.GetById;
using OrderFlow.Application.Products.Create;
using OrderFlow.Application.Products.GetAll;
using OrderFlow.Application.Products.GetById;
using OrderFlow.Application.Products.UpdatePrice;
using OrderFlow.Application.Products.UpdateStatus;

namespace OrderFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateProductUseCase>();
        services.AddScoped<GetProductsUseCase>();
        services.AddScoped<GetProductByIdUseCase>();
        services.AddScoped<UpdateProductPriceUseCase>();
        services.AddScoped<UpdateProductStatusUseCase>();

        services.AddScoped<CreateInventoryItemUseCase>();
        services.AddScoped<GetInventoryItemByProductIdUseCase>();
        services.AddScoped<IncreaseInventoryItemStockUseCase>();

        services.AddScoped<CreateOrderUseCase>();
        services.AddScoped<GetOrderByIdUseCase>();

        return services;
    }
}
