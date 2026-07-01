using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Id)
            .HasColumnName("id");

        builder.Property(product => product.Name)
            .HasColumnName("name")
            .HasMaxLength(Product.NameMaxLength)
            .IsRequired();

        builder.Property(product => product.Description)
            .HasColumnName("description")
            .HasMaxLength(Product.DescriptionMaxLength)
            .IsRequired();

        builder.Property(product => product.Price)
            .HasColumnName("price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(product => product.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(product => product.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(product => product.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
