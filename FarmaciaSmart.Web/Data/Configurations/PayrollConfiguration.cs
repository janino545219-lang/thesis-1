using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Data.Configurations;

public class PayrollConfiguration : IEntityTypeConfiguration<Payroll>
{
    public void Configure(EntityTypeBuilder<Payroll> builder)
    {
        builder.Property(p => p.BasicPay).HasColumnType("decimal(10,2)");
        builder.Property(p => p.GrossPay).HasColumnType("decimal(10,2)");
        builder.Property(p => p.TotalDeductions).HasColumnType("decimal(10,2)");
        builder.Property(p => p.NetPay).HasColumnType("decimal(10,2)");

        builder.HasIndex(p => new { p.EmployeeId, p.PeriodStart, p.PeriodEnd }).IsUnique();

        builder.HasOne(p => p.Employee)
            .WithMany()
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ProcessedByUser)
            .WithMany()
            .HasForeignKey(p => p.ProcessedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
