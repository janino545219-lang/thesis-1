using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Data.Configurations;

public class PayrollItemConfiguration : IEntityTypeConfiguration<PayrollItem>
{
    public void Configure(EntityTypeBuilder<PayrollItem> builder)
    {
        builder.Property(i => i.Description).HasMaxLength(150).IsRequired();
        builder.Property(i => i.Amount).HasColumnType("decimal(10,2)");

        builder.HasOne(i => i.Payroll)
            .WithMany(p => p.Items)
            .HasForeignKey(i => i.PayrollId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
