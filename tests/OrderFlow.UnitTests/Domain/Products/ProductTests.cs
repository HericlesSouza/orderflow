using OrderFlow.Domain.Entities;
using OrderFlow.UnitTests.Common.Builders;

namespace OrderFlow.UnitTests.Domain.Products;

[Trait("Layer", "Domain")]
[Trait("Feature", "Products")]
public sealed class ProductTests
{
    [Fact]
    public void Should_Create_Product_When_Input_Is_Valid()
    {
        var beforeCreation = DateTime.UtcNow;
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var afterCreation = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("Notebook", product.Name);
        Assert.Equal("Gaming notebook", product.Description);
        Assert.Equal(3500.00m, product.Price);
        Assert.True(product.IsActive);
        Assert.InRange(product.CreatedAt, beforeCreation, afterCreation);
        Assert.InRange(product.UpdatedAt, beforeCreation, afterCreation);
    }

    public static TheoryData<string, decimal, string, string> InvalidProducts => new()
    {
        { "", 3500.00m, "Gaming notebook", "Product name cannot be empty." },
        { "Notebook", 0, "Gaming notebook", "Product price must be greater than zero." },
        { "Notebook", 3500.00m, "", "Product description cannot be empty." }
    };

    [Theory]
    [MemberData(nameof(InvalidProducts))]
    public void Should_Throw_ArgumentException_When_Product_Input_Is_Invalid(
        string name,
        decimal price,
        string description,
        string expectedMessage)
    {
        var act = () => Product.Create(name, price, description);

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void Should_Deactivate_Product_And_Update_UpdatedAt_When_Product_Is_Active()
    {
        var product = new ProductBuilder().WithName("Notebook").WithDescription("Gaming notebook").WithPrice(3500.00m).Build();
        var beforeUpdate = DateTime.UtcNow;

        product.Deactivate();

        var afterUpdate = DateTime.UtcNow;

        Assert.False(product.IsActive);
        Assert.InRange(product.UpdatedAt, beforeUpdate, afterUpdate);
    }

    [Fact]
    public void Should_Activate_Product_And_Update_UpdatedAt_When_Product_Is_Inactive()
    {
        var product = new ProductBuilder().WithName("Notebook").WithDescription("Gaming notebook").WithPrice(3500.00m).Build();
        product.Deactivate();
        var beforeUpdate = DateTime.UtcNow;

        product.Activate();

        var afterUpdate = DateTime.UtcNow;

        Assert.True(product.IsActive);
        Assert.InRange(product.UpdatedAt, beforeUpdate, afterUpdate);
    }

    [Fact]
    public void Should_Change_Product_Price_And_Update_UpdatedAt_When_New_Price_Is_Different()
    {
        var product = new ProductBuilder().WithName("Notebook").WithDescription("Gaming notebook").WithPrice(3500.00m).Build();
        var beforeUpdate = DateTime.UtcNow;

        product.ChangePrice(4200.00m);

        var afterUpdate = DateTime.UtcNow;

        Assert.Equal(4200.00m, product.Price);
        Assert.InRange(product.UpdatedAt, beforeUpdate, afterUpdate);
    }

    [Fact]
    public void Should_Not_Update_UpdatedAt_When_Changing_To_Same_Price()
    {
        var product = new ProductBuilder().WithName("Notebook").WithDescription("Gaming notebook").WithPrice(3500.00m).Build();
        var originalUpdatedAt = product.UpdatedAt;

        product.ChangePrice(3500.00m);

        Assert.Equal(3500.00m, product.Price);
        Assert.Equal(originalUpdatedAt, product.UpdatedAt);
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_Changing_Product_Price_To_Invalid_Value()
    {
        var product = new ProductBuilder().WithName("Notebook").WithDescription("Gaming notebook").WithPrice(3500.00m).Build();
        var originalPrice = product.Price;
        var originalUpdatedAt = product.UpdatedAt;
        var act = () => product.ChangePrice(0);

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal("Product price must be greater than zero.", exception.Message);
        Assert.Equal(originalPrice, product.Price);
        Assert.Equal(originalUpdatedAt, product.UpdatedAt);
    }

    [Fact]
    public void Should_Set_Product_Status_To_Inactive_And_Update_UpdatedAt_When_Status_Changes()
    {
        var product = new ProductBuilder().WithName("Notebook").WithDescription("Gaming notebook").WithPrice(3500.00m).Build();
        var beforeUpdate = DateTime.UtcNow;

        product.SetStatus(false);

        var afterUpdate = DateTime.UtcNow;

        Assert.False(product.IsActive);
        Assert.InRange(product.UpdatedAt, beforeUpdate, afterUpdate);
    }

    [Fact]
    public void Should_Not_Update_UpdatedAt_When_Setting_The_Same_Status()
    {
        var product = new ProductBuilder().WithName("Notebook").WithDescription("Gaming notebook").WithPrice(3500.00m).Build();
        product.SetStatus(false);
        var originalUpdatedAt = product.UpdatedAt;

        product.SetStatus(false);

        Assert.False(product.IsActive);
        Assert.Equal(originalUpdatedAt, product.UpdatedAt);
    }
}
