using OrderFlow.Domain.Entities;

namespace OrderFlow.Tests;

public sealed class ProductTests
{
    [Fact]
    public void Should_Create_Product_When_Input_Is_Valid()
    {
        var product = Product.Create("Notebook", 3500.00m);

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("Notebook", product.Name);
        Assert.Equal(3500.00m, product.Price);
        Assert.False(product.Active);
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_Name_Is_Empty()
    {
        var act = () => Product.Create("", 3500.00m);

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal("Product name cannot be empty.", exception.Message);
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_Price_Is_Zero()
    {
        var act = () => Product.Create("Notebook", 0);

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal("Product price must be greater than zero.", exception.Message);
    }
}
