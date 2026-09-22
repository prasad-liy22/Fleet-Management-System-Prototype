using FleetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Infrastructure.Persistence.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> b)
    {
        b.ToTable("Vehicles", t =>
        {
            t.HasCheckConstraint("CK_Vehicles_Registration", "length(btrim(\"RegistrationNumber\")) > 0 AND \"RegistrationNumber\" = upper(btrim(\"RegistrationNumber\"))");
            t.HasCheckConstraint("CK_Vehicles_Year", "\"Year\" BETWEEN 1900 AND 2100");
            t.HasCheckConstraint("CK_Vehicles_Capacity", "\"Capacity\" > 0");
            t.HasCheckConstraint("CK_Vehicles_Odometer", "\"CurrentOdometer\" >= 0");
            t.HasCheckConstraint("CK_Vehicles_Status", "\"Status\" IN ('Available', 'OnTrip', 'UnderMaintenance')");
        });
        b.Property(x => x.RegistrationNumber).HasMaxLength(32).IsRequired();
        b.Property(x => x.Model).HasMaxLength(120).IsRequired();
        b.Property(x => x.Capacity).HasPrecision(12, 2);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.Version).IsRowVersion();
        b.HasIndex(x => x.RegistrationNumber).IsUnique();
        b.HasIndex(x => new { x.IsDeleted, x.Status });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
