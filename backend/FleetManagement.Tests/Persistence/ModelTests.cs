using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure.Identity;
using FleetManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace FleetManagement.Tests.Persistence;

public sealed class ModelTests
{
    private static FleetManagementDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<FleetManagementDbContext>()
                .UseNpgsql("Host=localhost;Database=model_only").Options,
            new TestAuditActor("test"), TimeProvider.System);

    [Fact]
    public void Model_has_identity_required_relationships_and_decimal_money()
    {
        using var db = CreateContext();
        Assert.NotNull(db.Model.FindEntityType(typeof(ApplicationUser)));
        var maintenance = db.Model.FindEntityType(typeof(MaintenanceLog))!;
        Assert.Equal(18, maintenance.FindProperty(nameof(MaintenanceLog.Cost))!.GetPrecision());
        Assert.Equal(2, maintenance.FindProperty(nameof(MaintenanceLog.Cost))!.GetScale());
        Assert.All(maintenance.GetForeignKeys(), fk =>
        {
            Assert.True(fk.IsRequired);
            Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
        });
        var order = db.Model.FindEntityType(typeof(Order))!;
        Assert.True(Assert.Single(order.GetForeignKeys()).IsRequired);
    }

    [Theory]
    [InlineData(typeof(Vehicle))]
    [InlineData(typeof(Driver))]
    [InlineData(typeof(Order))]
    [InlineData(typeof(Trip))]
    public void Concurrency_uses_postgres_xmin(Type type)
    {
        using var db = CreateContext();
        var property = db.Model.FindEntityType(type)!.FindProperty("Version")!;
        Assert.True(property.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, property.ValueGenerated);
        Assert.Equal("xmin", property.GetColumnName());
        Assert.Equal(typeof(uint), property.ClrType);
    }

    [Fact]
    public void Model_has_soft_delete_filters_and_active_resource_unique_indexes()
    {
        using var db = CreateContext();
        foreach (var type in new[] { typeof(Vehicle), typeof(Driver), typeof(CustomerCompany) })
            Assert.NotNull(db.Model.FindEntityType(type)!.GetQueryFilter());
        var indexes = db.Model.FindEntityType(typeof(Trip))!.GetIndexes()
            .Where(x => x.Name is "UX_Trips_ActiveVehicle" or "UX_Trips_ActiveDriver").ToArray();
        Assert.Equal(2, indexes.Length);
        Assert.All(indexes, index =>
        {
            Assert.True(index.IsUnique);
            Assert.Contains("'InProgress'", index.GetFilter());
        });
    }

    [Fact]
    public async Task Seeding_refuses_production_before_connecting()
    {
        await using var db = CreateContext();
        var seeder = new DevelopmentSeeder(db, new TestEnvironment(Environments.Production));
        await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync());
    }
}
