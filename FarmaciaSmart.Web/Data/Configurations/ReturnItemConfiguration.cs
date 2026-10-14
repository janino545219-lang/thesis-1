using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Data.Configurations;

public class ReturnItemConfiguration : IEntityTypeConfiguration<ReturnItem>
{
    public void Configure(EntityTypeBuilder<ReturnItem> builder)
    {
        builder.Property(ri => ri.RefundAmount).HasColumnType("decimal(10,2)");

        builder.HasOne(ri => ri.Return)
            .WithMany(r => r.Items)
            .HasForeignKey(ri => ri.ReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ri => ri.SaleItem)
            .WithMany()
            .HasForeignKey(ri => ri.SaleItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ri => ri.Product)
            .WithMany()
            .HasForeignKey(ri => ri.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
