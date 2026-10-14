using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Data.Configurations;

public class DamagedItemConfiguration : IEntityTypeConfiguration<DamagedItem>
{
    public void Configure(EntityTypeBuilder<DamagedItem> builder)
    {
        builder.Property(d => d.Reason).HasMaxLength(300).IsRequired();

        builder.HasOne(d => d.Product)
            .WithMany()
            .HasForeignKey(d => d.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Branch)
            .WithMany()
            .HasForeignKey(d => d.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.InventoryBatch)
            .WithMany()
            .HasForeignKey(d => d.InventoryBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ReportedByUser)
            .WithMany()
            .HasForeignKey(d => d.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.ReportedAt);
    }
}
