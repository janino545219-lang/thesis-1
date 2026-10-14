using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Brand).HasMaxLength(100);
        builder.Property(p => p.Unit).HasMaxLength(30);
        builder.Property(p => p.Barcode).HasMaxLength(50);
        builder.Property(p => p.Price).HasColumnType("decimal(10,2)");
        builder.Property(p => p.IsActive).HasDefaultValue(true);
        builder.Property(p => p.LowStockThreshold).HasDefaultValue(10);
        builder.Property(p => p.IsRecommendationEligible).HasDefaultValue(false);

        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.Barcode)
            .IsUnique()
            .HasFilter("[Barcode] IS NOT NULL");

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
