using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Data.Configurations;

public class RecommendationFeedbackConfiguration : IEntityTypeConfiguration<RecommendationFeedback>
{
    public void Configure(EntityTypeBuilder<RecommendationFeedback> builder)
    {
        builder.Property(f => f.Comment).HasMaxLength(500);

        builder.HasIndex(f => f.RecommendationItemId).IsUnique();

        builder.HasOne(f => f.RecommendationItem)
            .WithOne(i => i.Feedback)
            .HasForeignKey<RecommendationFeedback>(f => f.RecommendationItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Customer)
            .WithMany()
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
