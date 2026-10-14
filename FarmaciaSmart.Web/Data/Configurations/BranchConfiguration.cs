using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Data.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.Property(b => b.Name).HasMaxLength(150).IsRequired();
        builder.Property(b => b.Code).HasMaxLength(20).IsRequired();
        builder.Property(b => b.Address).HasMaxLength(300).IsRequired();
        builder.Property(b => b.ContactNumber).HasMaxLength(30);
        builder.Property(b => b.IsActive).HasDefaultValue(true);

        builder.HasIndex(b => b.Name).IsUnique();
        builder.HasIndex(b => b.Code).IsUnique();
    }
}
