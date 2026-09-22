using FleetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Infrastructure.Persistence.Configurations;

public sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> b)
    {
        b.ToTable("Trips", t =>
        {
            t.HasCheckConstraint("CK_Trips_Status", "\"Status\" IN ('Draft', 'Assigned', 'InProgress', 'Completed', 'Cancelled')");
            t.HasCheckConstraint("CK_Trips_ResourcePair", "(\"VehicleId\" IS NULL) = (\"DriverId\" IS NULL)");
            t.HasCheckConstraint("CK_Trips_Resources", """
                ("Status" <> 'Draft' OR ("VehicleId" IS NULL AND "DriverId" IS NULL)) AND
                ("Status" NOT IN ('Assigned', 'InProgress', 'Completed') OR ("VehicleId" IS NOT NULL AND "DriverId" IS NOT NULL))
                """);
            t.HasCheckConstraint("CK_Trips_Odometers", """
                ("StartOdometer" IS NULL OR "StartOdometer" >= 0) AND
                ("EndOdometer" IS NULL OR ("StartOdometer" IS NOT NULL AND "EndOdometer" >= "StartOdometer"))
                """);
            t.HasCheckConstraint("CK_Trips_Start", """
                ("StartOdometer" IS NULL) = ("StartDate" IS NULL) AND
                ("Status" NOT IN ('InProgress', 'Completed') OR "StartDate" IS NOT NULL) AND
                ("Status" NOT IN ('Draft', 'Assigned') OR "StartDate" IS NULL)
                """);
            t.HasCheckConstraint("CK_Trips_Return", """
                ("EndOdometer" IS NULL) = ("ActualReturnDate" IS NULL) AND
                ("ActualReturnDate" IS NULL OR ("StartDate" IS NOT NULL AND "ActualReturnDate" >= "StartDate")) AND
                ("Status" <> 'Completed' OR "ActualReturnDate" IS NOT NULL)
                """);
            t.HasCheckConstraint("CK_Trips_Cancellation", """
                "Status" <> 'Cancelled' OR ("CancellationReason" IS NOT NULL AND length(btrim("CancellationReason")) > 0)
                """);
        });
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.CancellationReason).HasMaxLength(2000);
        b.Property(x => x.Version).IsRowVersion();
        b.HasOne(x => x.Order).WithOne(x => x.Trip)
            .HasForeignKey<Trip>(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Vehicle).WithMany(x => x.Trips)
            .HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Driver).WithMany(x => x.Trips)
            .HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Restrict);
        const string active = "\"Status\" IN ('Assigned', 'InProgress')";
        b.HasIndex(x => x.VehicleId, "UX_Trips_ActiveVehicle").IsUnique().HasFilter(active);
        b.HasIndex(x => x.DriverId, "UX_Trips_ActiveDriver").IsUnique().HasFilter(active);
        b.HasIndex(x => new { x.VehicleId, x.StartDate });
        b.HasIndex(x => new { x.DriverId, x.StartDate });
        b.HasIndex(x => new { x.Status, x.StartDate });
    }
}
