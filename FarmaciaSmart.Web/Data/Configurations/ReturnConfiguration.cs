using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Data.Configurations;

public class ReturnConfiguration : IEntityTypeConfiguration<Return>
{
    public void Configure(EntityTypeBuilder<Return> builder)
    {
        builder.Property(r => r.ReturnNumber).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(300).IsRequired();
        builder.Property(r => r.TotalRefundAmount).HasColumnType("decimal(10,2)");

        builder.HasIndex(r => r.ReturnNumber).IsUnique();
        builder.HasIndex(r => r.ReturnDate);

        builder.HasOne(r => r.Sale)
            .WithMany()
            .HasForeignKey(r => r.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Branch)
            .WithMany()
            .HasForeignKey(r => r.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ProcessedByUser)
            .WithMany()
            .HasForeignKey(r => r.ProcessedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
