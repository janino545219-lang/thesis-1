using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmaciaSmart.Web.Models.Entities;

namespace FarmaciaSmart.Web.Data.Configurations;

public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.Property(a => a.CorrectionReason).HasMaxLength(300);

        builder.HasIndex(a => new { a.EmployeeId, a.AttendanceDate }).IsUnique();

        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CorrectedByUser)
            .WithMany()
            .HasForeignKey(a => a.CorrectedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
