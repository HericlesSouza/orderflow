using OrderFlow.Domain.Entities;

namespace OrderFlow.Tests;

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

    [Fact]
    public void Should_Throw_ArgumentException_When_Name_Is_Empty()
    {
        var act = () => Product.Create("", 3500.00m, "Gaming notebook");

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal("Product name cannot be empty.", exception.Message);
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_Price_Is_Zero()
    {
        var act = () => Product.Create("Notebook", 0, "Gaming notebook");

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal("Product price must be greater than zero.", exception.Message);
    }

    [Fact]
    public void Should_Deactivate_Product_And_Update_UpdatedAt()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var beforeUpdate = DateTime.UtcNow;

        product.Deactivate();

        var afterUpdate = DateTime.UtcNow;

        Assert.False(product.IsActive);
        Assert.InRange(product.UpdatedAt, beforeUpdate, afterUpdate);
    }

    [Fact]
    public void Should_Activate_Product_And_Update_UpdatedAt()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        product.Deactivate();
        var beforeUpdate = DateTime.UtcNow;

        product.Activate();

        var afterUpdate = DateTime.UtcNow;

        Assert.True(product.IsActive);
        Assert.InRange(product.UpdatedAt, beforeUpdate, afterUpdate);
    }
}
