using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items", table =>
        {
            table.HasCheckConstraint("ck_inventory_items_quantity_non_negative", "quantity >= 0");
            table.HasCheckConstraint("ck_inventory_items_reserved_quantity_non_negative", "reserved_quantity >= 0");
            table.HasCheckConstraint("ck_inventory_items_reserved_quantity_lte_quantity", "reserved_quantity <= quantity");
        });

        builder.HasKey(inventoryItem => inventoryItem.Id);

        builder.Ignore(inventoryItem => inventoryItem.AvailableQuantity);

        builder.Property(inventoryItem => inventoryItem.Id)
            .HasColumnName("id");

        builder.Property(inventoryItem => inventoryItem.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(inventoryItem => inventoryItem.Quantity)
            .HasColumnName("quantity")            
            .IsRequired();

        builder.Property(inventoryItem => inventoryItem.ReservedQuantity)
            .HasColumnName("reserved_quantity")
            .IsRequired();

        builder.Property(inventoryItem => inventoryItem.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(inventoryItem => inventoryItem.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne<Product>()
            .WithOne()
            .HasForeignKey<InventoryItem>(inventoryItem => inventoryItem.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
