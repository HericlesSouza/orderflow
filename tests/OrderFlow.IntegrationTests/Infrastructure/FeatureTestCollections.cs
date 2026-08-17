namespace OrderFlow.IntegrationTests.Infrastructure;

public static class IntegrationFeatures
{
    public const string Products = "Products";
    public const string Inventory = "Inventory";
    public const string Orders = "Orders";
}

[CollectionDefinition]
public sealed class ProductsApiCollection;

[CollectionDefinition]
public sealed class InventoryApiCollection;

[CollectionDefinition]
public sealed class OrdersApiCollection;
