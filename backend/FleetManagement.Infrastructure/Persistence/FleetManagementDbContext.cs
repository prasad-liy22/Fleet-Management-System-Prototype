using FleetManagement.Application.Abstractions;
using FleetManagement.Domain.Common;
using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FleetManagement.Infrastructure.Persistence;

public sealed class FleetManagementDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    private readonly IAuditActor actor;
    private readonly TimeProvider clock;

    public FleetManagementDbContext(
        DbContextOptions<FleetManagementDbContext> options,
        IAuditActor actor,
        TimeProvider clock) : base(options)
    {
        this.actor = actor;
        this.clock = clock;
        // Convert master deletes to updates before EF processes required dependents.
        ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;
        ChangeTracker.DeleteOrphansTiming = CascadeTiming.OnSaveChanges;
    }
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<CustomerCompany> CustomerCompanies => Set<CustomerCompany>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<DriverNote> DriverNotes => Set<DriverNote>();
    public DbSet<MaintenanceLog> MaintenanceLogs => Set<MaintenanceLog>();
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<ServiceSchedule> ServiceSchedules => Set<ServiceSchedule>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(FleetManagementDbContext).Assembly);
        foreach (var entityType in builder.Model.GetEntityTypes()
                     .Where(x => typeof(IAuditable).IsAssignableFrom(x.ClrType)))
        {
            var entity = builder.Entity(entityType.ClrType);
            entity.Property(nameof(IAuditable.CreatedBy)).HasMaxLength(128).IsRequired();
            entity.Property(nameof(IAuditable.UpdatedBy)).HasMaxLength(128).IsRequired();
            entity.Property(nameof(IAuditable.CreatedAt)).HasColumnType("timestamp with time zone");
            entity.Property(nameof(IAuditable.UpdatedAt)).HasColumnType("timestamp with time zone");
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PrepareChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepareChanges()
    {
        ChangeTracker.DetectChanges();
        var now = clock.GetUtcNow().ToUniversalTime();
        if (string.IsNullOrWhiteSpace(actor.ActorId) || actor.ActorId.Length > 128)
            throw new InvalidOperationException("A valid trusted audit actor is required.");

        foreach (var entry in ChangeTracker.Entries<IAuditable>().ToArray())
        {
            if (entry.State == EntityState.Deleted)
            {
                if (entry.Entity is not SoftDeletableEntity)
                    throw new InvalidOperationException("Business history cannot be physically deleted through this context.");
                // Update only the deletion flag and audit fields; preserve the loaded concurrency token.
                entry.State = EntityState.Unchanged;
                entry.Property(nameof(SoftDeletableEntity.IsDeleted)).CurrentValue = true;
                entry.Property(nameof(SoftDeletableEntity.IsDeleted)).IsModified = true;
            }

            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            if (entry.Entity is Vehicle vehicle)
                vehicle.RegistrationNumber = vehicle.RegistrationNumber.Trim().ToUpperInvariant();
            if (entry.Entity is Driver driver)
                driver.LicenceNumber = driver.LicenceNumber.Trim().ToUpperInvariant();

            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = actor.ActorId;
            }
            else
            {
                // Creation metadata is immutable even when a disconnected update includes it.
                entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
            }
            entry.Entity.UpdatedAt = now;
            entry.Entity.UpdatedBy = actor.ActorId;
        }
    }
}
