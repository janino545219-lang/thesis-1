using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(500).IsRequired();

        builder.HasOne(n => n.Branch)
            .WithMany()
            .HasForeignKey(n => n.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => n.CreatedAt);
        builder.HasIndex(n => new { n.Type, n.BranchId });
    }
}
