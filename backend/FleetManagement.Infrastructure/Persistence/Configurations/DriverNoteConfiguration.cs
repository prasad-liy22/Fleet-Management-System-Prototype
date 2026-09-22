using FleetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Infrastructure.Persistence.Configurations;

public sealed class DriverNoteConfiguration : IEntityTypeConfiguration<DriverNote>
{
    public void Configure(EntityTypeBuilder<DriverNote> b)
    {
        b.ToTable("DriverNotes", t =>
        {
            t.HasCheckConstraint("CK_DriverNotes_Note", "length(btrim(\"Note\")) > 0");
            t.HasCheckConstraint("CK_DriverNotes_Review", """
                (NOT "IsReviewed" AND "ReviewedAt" IS NULL AND "ReviewedBy" IS NULL) OR
                ("IsReviewed" AND "ReviewedAt" IS NOT NULL AND "ReviewedBy" IS NOT NULL AND length(btrim("ReviewedBy")) > 0)
                """);
        });
        b.Property(x => x.Note).HasMaxLength(4000).IsRequired();
        b.Property(x => x.ReviewedBy).HasMaxLength(128);
        b.HasOne(x => x.Trip).WithMany(x => x.DriverNotes)
            .HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Driver).WithMany(x => x.Notes)
            .HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.RequiresAttention, x.IsReviewed, x.CreatedAt });
    }
}
