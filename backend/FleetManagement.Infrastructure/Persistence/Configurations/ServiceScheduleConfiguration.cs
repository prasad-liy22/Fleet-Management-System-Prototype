using FleetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Infrastructure.Persistence.Configurations;

public sealed class ServiceScheduleConfiguration : IEntityTypeConfiguration<ServiceSchedule>
{
    public void Configure(EntityTypeBuilder<ServiceSchedule> b)
    {
        b.ToTable("ServiceSchedules", t =>
        {
            t.HasCheckConstraint("CK_ServiceSchedules_Threshold", "\"DueDate\" IS NOT NULL OR \"DueOdometer\" IS NOT NULL");
            t.HasCheckConstraint("CK_ServiceSchedules_Odometer", "\"DueOdometer\" IS NULL OR \"DueOdometer\" >= 0");
            t.HasCheckConstraint("CK_ServiceSchedules_Completion", """
                ("IsCompleted" AND "CompletedAt" IS NOT NULL) OR
                (NOT "IsCompleted" AND "CompletedAt" IS NULL AND "CompletionLogId" IS NULL)
                """);
        });
        b.HasOne(x => x.Vehicle).WithMany(x => x.ServiceSchedules)
            .HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ServiceType).WithMany(x => x.ServiceSchedules)
            .HasForeignKey(x => x.ServiceTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CompletionLog).WithMany()
            .HasForeignKey(x => x.CompletionLogId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.IsCompleted, x.DueDate });
        b.HasIndex(x => new { x.VehicleId, x.IsCompleted, x.DueOdometer });
    }
}
