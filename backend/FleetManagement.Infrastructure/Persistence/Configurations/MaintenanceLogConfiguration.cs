using FleetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceLogConfiguration : IEntityTypeConfiguration<MaintenanceLog>
{
    public void Configure(EntityTypeBuilder<MaintenanceLog> b)
    {
        b.ToTable("MaintenanceLogs", t =>
        {
            t.HasCheckConstraint("CK_MaintenanceLogs_Cost", "\"Cost\" >= 0");
            t.HasCheckConstraint("CK_MaintenanceLogs_Odometer", "\"Odometer\" >= 0");
        });
        b.Property(x => x.Cost).HasPrecision(18, 2);
        b.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        b.Property(x => x.Parts).HasMaxLength(4000);
        b.Property(x => x.ReceiptBlobName).HasMaxLength(1024);
        b.HasOne(x => x.Vehicle).WithMany(x => x.MaintenanceLogs)
            .HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ServiceType).WithMany(x => x.MaintenanceLogs)
            .HasForeignKey(x => x.ServiceTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.VehicleId, x.ServiceDate });
    }
}
