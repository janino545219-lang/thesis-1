using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Data.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.Property(s => s.ReceiptNumber).HasMaxLength(20).IsRequired();
        builder.Property(s => s.SubTotal).HasColumnType("decimal(10,2)");
        builder.Property(s => s.DiscountAmount).HasColumnType("decimal(10,2)");
        builder.Property(s => s.TotalAmount).HasColumnType("decimal(10,2)");

        builder.HasIndex(s => s.ReceiptNumber).IsUnique();
        builder.HasIndex(s => s.SaleDate);

        builder.HasOne(s => s.Branch)
            .WithMany()
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Customer)
            .WithMany()
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.CashierUser)
            .WithMany()
            .HasForeignKey(s => s.CashierUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
