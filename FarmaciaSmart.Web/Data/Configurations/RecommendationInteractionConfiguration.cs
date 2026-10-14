using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Data.Configurations;

public class RecommendationInteractionConfiguration : IEntityTypeConfiguration<RecommendationInteraction>
{
    public void Configure(EntityTypeBuilder<RecommendationInteraction> builder)
    {
        builder.HasIndex(i => new { i.RecommendationId, i.InteractionType });

        builder.HasOne(i => i.Customer)
            .WithMany()
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Recommendation)
            .WithMany()
            .HasForeignKey(i => i.RecommendationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
