using FleetManagement.Domain.Entities;
using FleetManagement.Domain.Enums;
using FleetManagement.Infrastructure.Identity;
using FleetManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Xunit;

namespace FleetManagement.Tests.Persistence;

public sealed class PersistenceTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private static Vehicle Vehicle() => new()
    {
        RegistrationNumber = $"T-{Guid.NewGuid():N}"[..30],
        Model = "Fictional test vehicle", Year = 2024, Capacity = 18000m, CurrentOdometer = 100
    };

    private static Driver Driver() => new()
    {
        Name = "Fictional test driver", LicenceNumber = $"TEST-{Guid.NewGuid():N}", Contact = "TEST-NOT-A-PHONE"
    };

    private static Order Order() => new()
    {
        CustomerCompany = new CustomerCompany
        {
            CompanyName = "Fictional test company", ContactPerson = "Sample person",
            Telephone = "TEST-NOT-A-PHONE", Email = "test@example.invalid", Address = "Sample yard"
        },
        PickupLocation = "Sample pickup", DeliveryLocation = "Sample delivery", RequiredDate = new DateOnly(2026, 1, 1)
    };

    private static void AssertDatabaseError(DbUpdateException exception, string sqlState) =>
        Assert.Equal(sqlState, Assert.IsType<PostgresException>(exception.InnerException).SqlState);

    [PostgreSqlFact]
    public async Task Registrations_are_normalized_and_unique_even_after_soft_delete()
    {
        await using var db = fixture.CreateContext();
        var original = Vehicle();
        db.Add(original);
        await db.SaveChangesAsync();
        db.Remove(original);
        await db.SaveChangesAsync();
        var duplicate = Vehicle();
        duplicate.RegistrationNumber = $" {original.RegistrationNumber.ToLowerInvariant()} ";
        db.Add(duplicate);
        AssertDatabaseError(await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()), PostgresErrorCodes.UniqueViolation);
    }

    [PostgreSqlFact]
    public async Task Licences_are_normalized_and_unique()
    {
        await using var db = fixture.CreateContext();
        var original = Driver();
        db.Add(original);
        await db.SaveChangesAsync();
        var duplicate = Driver();
        duplicate.LicenceNumber = $" {original.LicenceNumber.ToLowerInvariant()} ";
        db.Add(duplicate);
        AssertDatabaseError(await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()), PostgresErrorCodes.UniqueViolation);
    }

    [PostgreSqlFact]
    public async Task Remove_soft_deletes_all_master_types_and_preserves_history()
    {
        await using var db = fixture.CreateContext();
        var vehicle = Vehicle();
        var driver = Driver();
        var order = Order();
        var trip = new Trip { Order = order, Vehicle = vehicle, Driver = driver, Status = TripStatus.Assigned };
        db.Add(trip);
        await db.SaveChangesAsync();
        db.RemoveRange(vehicle, driver, order.CustomerCompany);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Assert.False(await db.Vehicles.AnyAsync(x => x.Id == vehicle.Id));
        Assert.False(await db.Drivers.AnyAsync(x => x.Id == driver.Id));
        Assert.False(await db.CustomerCompanies.AnyAsync(x => x.Id == order.CustomerCompanyId));
        Assert.True((await db.Vehicles.IgnoreQueryFilters().SingleAsync(x => x.Id == vehicle.Id)).IsDeleted);
        Assert.True((await db.Drivers.IgnoreQueryFilters().SingleAsync(x => x.Id == driver.Id)).IsDeleted);
        Assert.True((await db.CustomerCompanies.IgnoreQueryFilters().SingleAsync(x => x.Id == order.CustomerCompanyId)).IsDeleted);
        Assert.True(await db.Trips.AnyAsync(x => x.Id == trip.Id));
        var historical = await db.Trips.IgnoreQueryFilters().Include(x => x.Vehicle)
            .Include(x => x.Driver).Include(x => x.Order).ThenInclude(x => x.CustomerCompany)
            .SingleAsync(x => x.Id == trip.Id);
        Assert.Equal(vehicle.RegistrationNumber, historical.Vehicle!.RegistrationNumber);
        Assert.True(historical.Order.CustomerCompany.IsDeleted);
    }

    [PostgreSqlFact]
    public async Task Audit_uses_trusted_actor_utc_and_preserves_creation_fields()
    {
        var created = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updated = created.AddHours(1);
        Guid id;
        await using (var db = fixture.CreateContext("test:creator", new TestClock(created)))
        {
            var vehicle = Vehicle();
            vehicle.CreatedBy = "untrusted";
            db.Add(vehicle);
            db.SaveChanges(); // Exercise the synchronous save path too.
            id = vehicle.Id;
            Assert.Equal(created, vehicle.CreatedAt);
            Assert.Equal("test:creator", vehicle.CreatedBy);
        }
        await using (var db = fixture.CreateContext("test:editor", new TestClock(updated)))
        {
            var vehicle = await db.Vehicles.SingleAsync(x => x.Id == id);
            vehicle.Model = "Updated fictional model";
            vehicle.CreatedBy = "spoofed";
            vehicle.CreatedAt = updated;
            await db.SaveChangesAsync();
        }
        await using var verify = fixture.CreateContext();
        var stored = await verify.Vehicles.SingleAsync(x => x.Id == id);
        Assert.Equal("test:creator", stored.CreatedBy);
        Assert.Equal(created, stored.CreatedAt);
        Assert.Equal(updated, stored.UpdatedAt);
        Assert.Equal(TimeSpan.Zero, stored.UpdatedAt.Offset);
        Assert.Equal("test:editor", stored.UpdatedBy);
    }

    [PostgreSqlFact]
    public async Task Required_customer_foreign_key_is_enforced()
    {
        await using var db = fixture.CreateContext();
        db.Add(new Order { CustomerCompanyId = Guid.NewGuid(), PickupLocation = "Test", DeliveryLocation = "Test", RequiredDate = new DateOnly(2026, 1, 1) });
        AssertDatabaseError(await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()), PostgresErrorCodes.ForeignKeyViolation);
    }

    [PostgreSqlFact]
    public async Task Historical_foreign_keys_block_physical_deletion()
    {
        await using var db = fixture.CreateContext();
        var log = new MaintenanceLog
        {
            Vehicle = Vehicle(), ServiceType = new ServiceType { Name = $"Test-{Guid.NewGuid():N}" },
            Description = "Fictional inspection", Cost = 1250.50m, Odometer = 100, ServiceDate = new DateOnly(2026, 1, 1)
        };
        db.Add(log);
        await db.SaveChangesAsync();
        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"Vehicles\" WHERE \"Id\" = {log.VehicleId}"));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
        db.ChangeTracker.Clear();
        Assert.Equal(1250.50m, (await db.MaintenanceLogs.SingleAsync(x => x.Id == log.Id)).Cost);
    }

    [PostgreSqlFact]
    public async Task Business_history_cannot_be_removed_through_context()
    {
        await using var db = fixture.CreateContext();
        var order = Order();
        db.Add(order);
        await db.SaveChangesAsync();
        db.Remove(order);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task Negative_maintenance_cost_is_rejected()
    {
        await using var db = fixture.CreateContext();
        db.Add(new MaintenanceLog
        {
            Vehicle = Vehicle(), ServiceType = new ServiceType { Name = $"Test-{Guid.NewGuid():N}" },
            Description = "Fictional inspection", Cost = -1m, Odometer = 100, ServiceDate = new DateOnly(2026, 1, 1)
        });
        AssertDatabaseError(await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()), PostgresErrorCodes.CheckViolation);
    }

    [PostgreSqlTheory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Schedules_accept_date_odometer_or_both(bool withDate, bool withOdometer)
    {
        await using var db = fixture.CreateContext();
        db.Add(new ServiceSchedule
        {
            Vehicle = Vehicle(), ServiceType = new ServiceType { Name = $"Test-{Guid.NewGuid():N}" },
            DueDate = withDate ? new DateOnly(2026, 12, 1) : null, DueOdometer = withOdometer ? 1000 : null
        });
        await db.SaveChangesAsync();
    }

    [PostgreSqlFact]
    public async Task Schedule_requires_at_least_one_threshold()
    {
        await using var db = fixture.CreateContext();
        db.Add(new ServiceSchedule { Vehicle = Vehicle(), ServiceType = new ServiceType { Name = $"Test-{Guid.NewGuid():N}" } });
        AssertDatabaseError(await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()), PostgresErrorCodes.CheckViolation);
    }

    [PostgreSqlTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Xmin_rejects_stale_vehicle_and_driver_updates(bool useVehicle)
    {
        await using var first = fixture.CreateContext();
        await using var second = fixture.CreateContext();
        if (useVehicle)
        {
            var vehicle = Vehicle();
            first.Add(vehicle);
            await first.SaveChangesAsync();
            var stale = await second.Vehicles.SingleAsync(x => x.Id == vehicle.Id);
            vehicle.Status = VehicleStatus.OnTrip;
            stale.Status = VehicleStatus.UnderMaintenance;
        }
        else
        {
            var driver = Driver();
            first.Add(driver);
            await first.SaveChangesAsync();
            var stale = await second.Drivers.SingleAsync(x => x.Id == driver.Id);
            driver.Status = DriverStatus.OnTrip;
            stale.Status = DriverStatus.Inactive;
        }
        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [PostgreSqlTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Partial_indexes_prevent_duplicate_active_resources(bool sameVehicle)
    {
        await using var db = fixture.CreateContext();
        var first = new Trip { Order = Order(), Vehicle = Vehicle(), Driver = Driver(), Status = TripStatus.Assigned };
        db.Add(first);
        await db.SaveChangesAsync();
        var second = new Trip
        {
            Order = Order(), Vehicle = sameVehicle ? first.Vehicle : Vehicle(),
            Driver = sameVehicle ? Driver() : first.Driver, Status = TripStatus.InProgress,
            StartOdometer = 100, StartDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };
        db.Add(second);
        AssertDatabaseError(await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()), PostgresErrorCodes.UniqueViolation);
    }

    [PostgreSqlFact]
    public async Task Cancelled_history_does_not_reserve_resources()
    {
        await using var db = fixture.CreateContext();
        var first = new Trip
        {
            Order = Order(), Vehicle = Vehicle(), Driver = Driver(), Status = TripStatus.Cancelled, CancellationReason = "Fictional cancellation"
        };
        db.Add(first);
        await db.SaveChangesAsync();
        db.Add(new Trip { Order = Order(), Vehicle = first.Vehicle, Driver = first.Driver, Status = TripStatus.Assigned });
        await db.SaveChangesAsync();
    }

    [PostgreSqlFact]
    public async Task Identity_user_can_link_to_only_one_driver()
    {
        await using var db = fixture.CreateContext();
        var user = new ApplicationUser { Id = Guid.NewGuid(), DisplayName = "Sample account", UserName = $"test-{Guid.NewGuid():N}" };
        db.Users.Add(user);
        var driver = Driver();
        driver.ApplicationUserId = user.Id;
        db.Add(driver);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear(); // Test the database constraint without relationship fix-up.
        var duplicate = Driver();
        duplicate.ApplicationUserId = user.Id;
        db.Add(duplicate);
        AssertDatabaseError(await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()), PostgresErrorCodes.UniqueViolation);
    }

    [PostgreSqlFact]
    public async Task Seed_is_repeatable_preserves_edits_and_creates_no_accounts()
    {
        await using var db = fixture.CreateContext("system:development-seed");
        var usersBefore = await db.Users.CountAsync();
        var seeder = new DevelopmentSeeder(db, new TestEnvironment(Environments.Development));
        await seeder.SeedAsync();
        var countBefore = await db.Trips.CountAsync();
        var vehicle = await db.Vehicles.SingleAsync(x => x.RegistrationNumber == "DEMO-TRUCK-001");
        vehicle.Model = "Edited sample model";
        await db.SaveChangesAsync();
        await seeder.SeedAsync();
        Assert.Equal(countBefore, await db.Trips.CountAsync());
        Assert.Equal("Edited sample model", vehicle.Model);
        Assert.Equal(usersBefore, await db.Users.CountAsync());
        Assert.Equal(3, await db.Vehicles.CountAsync(x => x.RegistrationNumber.StartsWith("DEMO-")));
    }

    [PostgreSqlFact]
    public async Task Migration_is_applied_and_generated_script_is_idempotent()
    {
        await using var db = fixture.CreateContext();
        Assert.Single(await db.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        var script = db.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(script, connection);
        await command.ExecuteNonQueryAsync();
        await command.ExecuteNonQueryAsync();
    }
}
