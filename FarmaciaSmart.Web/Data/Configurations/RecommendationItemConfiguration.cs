using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Data.Configurations;

public class RecommendationItemConfiguration : IEntityTypeConfiguration<RecommendationItem>
{
    public void Configure(EntityTypeBuilder<RecommendationItem> builder)
    {
        builder.Property(i => i.Explanation).HasMaxLength(1000).IsRequired();
        builder.Property(i => i.Warning).HasMaxLength(500);
        builder.Property(i => i.Score).HasColumnType("decimal(10,2)");

        builder.HasOne(i => i.Recommendation)
            .WithMany(r => r.Items)
            .HasForeignKey(i => i.RecommendationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.RecommendationRule)
            .WithMany()
            .HasForeignKey(i => i.RecommendationRuleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
