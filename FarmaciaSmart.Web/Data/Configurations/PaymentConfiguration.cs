using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(p => p.AmountTendered).HasColumnType("decimal(10,2)");
        builder.Property(p => p.ChangeAmount).HasColumnType("decimal(10,2)");

        builder.HasOne(p => p.Sale)
            .WithOne(s => s.Payment)
            .HasForeignKey<Payment>(p => p.SaleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
