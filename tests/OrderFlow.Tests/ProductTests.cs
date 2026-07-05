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
    public void Should_Throw_ArgumentException_When_Description_Is_Empty()
    {
        var act = () => Product.Create("Notebook", 3500.00m, "");

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal("Product description cannot be empty.", exception.Message);
    }

    [Fact]
    public void Should_Update_Product_When_Input_Is_Valid()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var beforeUpdate = DateTime.UtcNow;

        product.Update("Notebook Pro", 4200.00m, "Upgraded gaming notebook");

        var afterUpdate = DateTime.UtcNow;

        Assert.Equal("Notebook Pro", product.Name);
        Assert.Equal(4200.00m, product.Price);
        Assert.Equal("Upgraded gaming notebook", product.Description);
        Assert.InRange(product.UpdatedAt, beforeUpdate, afterUpdate);
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_Updating_Product_With_Invalid_Name()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var originalName = product.Name;
        var originalPrice = product.Price;
        var originalDescription = product.Description;
        var originalUpdatedAt = product.UpdatedAt;
        var act = () => product.Update("", 4200.00m, "Upgraded gaming notebook");

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal("Product name cannot be empty.", exception.Message);
        Assert.Equal(originalName, product.Name);
        Assert.Equal(originalPrice, product.Price);
        Assert.Equal(originalDescription, product.Description);
        Assert.Equal(originalUpdatedAt, product.UpdatedAt);
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_Updating_Product_With_Invalid_Price()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var originalName = product.Name;
        var originalPrice = product.Price;
        var originalDescription = product.Description;
        var originalUpdatedAt = product.UpdatedAt;
        var act = () => product.Update("Notebook Pro", 0, "Upgraded gaming notebook");

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal("Product price must be greater than zero.", exception.Message);
        Assert.Equal(originalName, product.Name);
        Assert.Equal(originalPrice, product.Price);
        Assert.Equal(originalDescription, product.Description);
        Assert.Equal(originalUpdatedAt, product.UpdatedAt);
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_Updating_Product_With_Invalid_Description()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var originalName = product.Name;
        var originalPrice = product.Price;
        var originalDescription = product.Description;
        var originalUpdatedAt = product.UpdatedAt;
        var act = () => product.Update("Notebook Pro", 4200.00m, "");

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal("Product description cannot be empty.", exception.Message);
        Assert.Equal(originalName, product.Name);
        Assert.Equal(originalPrice, product.Price);
        Assert.Equal(originalDescription, product.Description);
        Assert.Equal(originalUpdatedAt, product.UpdatedAt);
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

    [Fact]
    public void Should_Change_Product_Price_And_Update_UpdatedAt()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var beforeUpdate = DateTime.UtcNow;

        product.ChangePrice(4200.00m);

        var afterUpdate = DateTime.UtcNow;

        Assert.Equal(4200.00m, product.Price);
        Assert.InRange(product.UpdatedAt, beforeUpdate, afterUpdate);
    }

    [Fact]
    public void Should_Not_Update_UpdatedAt_When_Changing_To_Same_Price()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var originalUpdatedAt = product.UpdatedAt;

        product.ChangePrice(3500.00m);

        Assert.Equal(3500.00m, product.Price);
        Assert.Equal(originalUpdatedAt, product.UpdatedAt);
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_Changing_Product_Price_To_Invalid_Value()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var originalPrice = product.Price;
        var originalUpdatedAt = product.UpdatedAt;
        var act = () => product.ChangePrice(0);

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal("Product price must be greater than zero.", exception.Message);
        Assert.Equal(originalPrice, product.Price);
        Assert.Equal(originalUpdatedAt, product.UpdatedAt);
    }

    [Fact]
    public void Should_Set_Product_Status_To_Inactive_And_Update_UpdatedAt()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var beforeUpdate = DateTime.UtcNow;

        product.SetStatus(false);

        var afterUpdate = DateTime.UtcNow;

        Assert.False(product.IsActive);
        Assert.InRange(product.UpdatedAt, beforeUpdate, afterUpdate);
    }

    [Fact]
    public void Should_Not_Update_UpdatedAt_When_Setting_The_Same_Status()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        product.SetStatus(false);
        var originalUpdatedAt = product.UpdatedAt;

        product.SetStatus(false);

        Assert.False(product.IsActive);
        Assert.Equal(originalUpdatedAt, product.UpdatedAt);
    }
}
