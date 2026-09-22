using FleetManagement.Domain.Common;
using FleetManagement.Domain.Entities;
using FleetManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace FleetManagement.Infrastructure.Persistence;

/// <summary>Opt-in, additive fictional fixtures. Never overwrites existing records.</summary>
public sealed class DevelopmentSeeder(FleetManagementDbContext db, IHostEnvironment environment)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("Fictional seed data is restricted to Development.");
        if ((await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
            throw new InvalidOperationException("Apply database migrations before seeding.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Serializes concurrent seed runs in this database without persistent lock records.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7462012301)", cancellationToken);

        var customer = new CustomerCompany
        {
            Id = Id(1), CompanyName = "Fictional Mooncrate Logistics",
            ContactPerson = "Sample Contact Alpha", Telephone = "DEMO-NOT-A-PHONE-01",
            Email = "dispatch@mooncrate.example", Address = "Sample Yard A, Fictional District"
        };
        var customer2 = new CustomerCompany
        {
            Id = Id(2), CompanyName = "Fictional Cloudbrick Works",
            ContactPerson = "Sample Contact Beta", Telephone = "DEMO-NOT-A-PHONE-02",
            Email = "office@cloudbrick.example", Address = "Sample Yard B, Fictional District"
        };
        await AddMissingAsync(new[] { customer, customer2 }, cancellationToken);

        var vehicles = new[]
        {
            new Vehicle { Id = Id(10), RegistrationNumber = "DEMO-TRUCK-001", Model = "Fictional Hauler A", Year = 2023, Capacity = 18000m, CurrentOdometer = 12000 },
            new Vehicle { Id = Id(11), RegistrationNumber = "DEMO-TRUCK-002", Model = "Fictional Hauler B", Year = 2024, Capacity = 22000m, CurrentOdometer = 8000, Status = VehicleStatus.OnTrip },
            new Vehicle { Id = Id(12), RegistrationNumber = "DEMO-TRUCK-003", Model = "Fictional Hauler C", Year = 2022, Capacity = 16000m, CurrentOdometer = 24000, Status = VehicleStatus.UnderMaintenance }
        };
        await AddMissingAsync(vehicles, cancellationToken);
        var drivers = new[]
        {
            new Driver { Id = Id(20), Name = "Sample Driver Alpha", LicenceNumber = "DEMO-LICENCE-001", Contact = "DEMO-NOT-A-PHONE-03" },
            new Driver { Id = Id(21), Name = "Sample Driver Beta", LicenceNumber = "DEMO-LICENCE-002", Contact = "DEMO-NOT-A-PHONE-04", Status = DriverStatus.OnTrip }
        };
        await AddMissingAsync(drivers, cancellationToken);
        await AddMissingAsync(new[]
        {
            new ServiceType { Id = Id(30), Name = "Sample routine inspection", Description = "Fictional inspection reference" },
            new ServiceType { Id = Id(31), Name = "Sample lubrication service", Description = "Fictional lubrication reference" },
            new ServiceType { Id = Id(32), Name = "Sample brake inspection", Description = "Fictional brake inspection reference" }
        }, cancellationToken);
        await AddMissingAsync(new[]
        {
            new Order { Id = Id(40), CustomerCompanyId = customer.Id, PickupLocation = "Sample Depot A", DeliveryLocation = "Sample Depot B", RequiredDate = new DateOnly(2026, 10, 1), Notes = "Fictional draft order" },
            new Order { Id = Id(41), CustomerCompanyId = customer2.Id, PickupLocation = "Sample Depot C", DeliveryLocation = "Sample Depot D", RequiredDate = new DateOnly(2026, 10, 2), Status = OrderStatus.Assigned },
            new Order { Id = Id(42), CustomerCompanyId = customer.Id, PickupLocation = "Sample Depot E", DeliveryLocation = "Sample Depot F", RequiredDate = new DateOnly(2026, 1, 10), Status = OrderStatus.Completed }
        }, cancellationToken);
        await AddMissingAsync(new[]
        {
            new Trip { Id = Id(50), OrderId = Id(40) },
            new Trip { Id = Id(51), OrderId = Id(41), VehicleId = Id(11), DriverId = Id(21), Status = TripStatus.Assigned },
            new Trip
            {
                Id = Id(52), OrderId = Id(42), VehicleId = Id(10), DriverId = Id(20), Status = TripStatus.Completed,
                StartOdometer = 11500, EndOdometer = 12000,
                StartDate = new DateTimeOffset(2026, 1, 10, 6, 0, 0, TimeSpan.Zero),
                ActualReturnDate = new DateTimeOffset(2026, 1, 10, 18, 0, 0, TimeSpan.Zero)
            }
        }, cancellationToken);
        await AddMissingAsync(new[]
        {
            new DriverNote { Id = Id(60), TripId = Id(52), DriverId = Id(20), Note = "Fictional observation: inspect dashboard warning indicator." }
        }, cancellationToken);
        await AddMissingAsync(new[]
        {
            new MaintenanceLog { Id = Id(70), VehicleId = Id(10), ServiceTypeId = Id(30), Description = "Fictional inspection completed", Parts = "Sample seal", Cost = 1250.50m, Odometer = 11000, ServiceDate = new DateOnly(2026, 1, 5) },
            new MaintenanceLog { Id = Id(71), VehicleId = Id(12), ServiceTypeId = Id(32), Description = "Fictional brake inspection", Cost = 2500m, Odometer = 24000, ServiceDate = new DateOnly(2026, 1, 6) }
        }, cancellationToken);
        await AddMissingAsync(new[]
        {
            new ServiceSchedule { Id = Id(80), VehicleId = Id(10), ServiceTypeId = Id(30), DueDate = new DateOnly(2026, 1, 5), IsCompleted = true, CompletedAt = new DateTimeOffset(2026, 1, 5, 12, 0, 0, TimeSpan.Zero), CompletionLogId = Id(70) },
            new ServiceSchedule { Id = Id(81), VehicleId = Id(10), ServiceTypeId = Id(30), DueDate = new DateOnly(2026, 12, 1) },
            new ServiceSchedule { Id = Id(82), VehicleId = Id(11), ServiceTypeId = Id(31), DueOdometer = 10000 },
            new ServiceSchedule { Id = Id(83), VehicleId = Id(12), ServiceTypeId = Id(32), DueDate = new DateOnly(2026, 1, 1), DueOdometer = 23000 }
        }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task AddMissingAsync<T>(IEnumerable<T> records, CancellationToken cancellationToken)
        where T : AuditableEntity
    {
        foreach (var record in records)
            if (!await db.Set<T>().IgnoreQueryFilters().AnyAsync(x => x.Id == record.Id, cancellationToken))
                db.Add(record);
    }

    private static Guid Id(int value) => Guid.Parse($"00000000-0000-4000-8000-{value:D12}");
}
