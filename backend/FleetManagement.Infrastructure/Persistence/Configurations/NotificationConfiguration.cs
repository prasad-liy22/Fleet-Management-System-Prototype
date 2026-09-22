using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notifications", t =>
        {
            t.HasCheckConstraint("CK_Notifications_Attempts", "\"AttemptCount\" >= 0");
            t.HasCheckConstraint("CK_Notifications_Sent", "\"IsSent\" = (\"SentAt\" IS NOT NULL)");
            t.HasCheckConstraint("CK_Notifications_RelatedRecord", "(\"RelatedRecordId\" IS NULL) = (\"RelatedRecordType\" IS NULL)");
        });
        b.Property(x => x.Recipient).HasMaxLength(254).IsRequired();
        b.Property(x => x.Subject).HasMaxLength(200).IsRequired();
        b.Property(x => x.Body).HasMaxLength(10000).IsRequired();
        b.Property(x => x.RelatedRecordType).HasMaxLength(64);
        b.Property(x => x.LastError).HasMaxLength(2000);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.IsSent, x.CreatedAt });
        b.HasIndex(x => new { x.RelatedRecordType, x.RelatedRecordId });
    }
}
