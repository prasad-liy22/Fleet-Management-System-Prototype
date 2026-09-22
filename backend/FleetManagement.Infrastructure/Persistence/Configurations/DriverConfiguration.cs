using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Infrastructure.Persistence.Configurations;

public sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> b)
    {
        b.ToTable("Drivers", t =>
        {
            t.HasCheckConstraint("CK_Drivers_Licence", "length(btrim(\"LicenceNumber\")) > 0 AND \"LicenceNumber\" = upper(btrim(\"LicenceNumber\"))");
            t.HasCheckConstraint("CK_Drivers_Status", "\"Status\" IN ('Available', 'OnTrip', 'Inactive')");
        });
        b.Property(x => x.Name).HasMaxLength(160).IsRequired();
        b.Property(x => x.LicenceNumber).HasMaxLength(64).IsRequired();
        b.Property(x => x.Contact).HasMaxLength(40).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.Version).IsRowVersion();
        b.HasIndex(x => x.LicenceNumber).IsUnique();
        b.HasIndex(x => new { x.IsDeleted, x.Status });
        b.HasOne<ApplicationUser>().WithOne().HasForeignKey<Driver>(x => x.ApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
