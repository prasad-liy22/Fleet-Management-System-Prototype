using FleetManagement.Application.Access;
using FleetManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace FleetManagement.Infrastructure.Identity;

public sealed class IdentitySeeder(
    FleetManagementDbContext db, RoleManager<IdentityRole<Guid>> roles,
    UserManager<ApplicationUser> users, IConfiguration configuration, IHostEnvironment environment)
{
    public static IReadOnlyDictionary<string, string> DemoEmails { get; } = new Dictionary<string, string>
    {
        [FleetRoles.FleetAdministrator] = "admin@fleet.example",
        [FleetRoles.OperationsCoordinator] = "operations@fleet.example",
        [FleetRoles.Mechanic] = "mechanic@fleet.example",
        [FleetRoles.Driver] = "driver@fleet.example",
        [FleetRoles.FleetOwner] = "owner@fleet.example"
    };

    public async Task SeedRolesAsync()
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await IdentityMutationLock.AcquireAsync(db);
        foreach (var role in FleetRoles.All)
            if (!await roles.RoleExistsAsync(role))
                IdentityResults.RequireSuccess(await roles.CreateAsync(new IdentityRole<Guid>(role)));
        await transaction.CommitAsync();
    }

    public async Task SeedDevelopmentUsersAsync()
    {
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("Demo accounts are restricted to Development.");
        var password = configuration["Seed:Password"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Set Seed__Password to a strong, private development password.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        await IdentityMutationLock.AcquireAsync(db);
        foreach (var (role, email) in DemoEmails)
        {
            if (await users.FindByEmailAsync(email) is not null) continue;
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(), UserName = email, Email = email,
                DisplayName = $"Sample {role}", IsActive = true
            };
            if (role == FleetRoles.Driver)
            {
                var driverId = Guid.Parse("00000000-0000-4000-8000-000000000020");
                var driver = await db.Drivers.SingleOrDefaultAsync(x => x.Id == driverId)
                    ?? throw new InvalidOperationException("Run the fictional fleet seeder before demo account seeding.");
                if (driver.ApplicationUserId is not null)
                    throw new InvalidOperationException("The demo driver is linked to another account.");
                IdentityResults.RequireSuccess(await users.CreateAsync(user, password));
                driver.ApplicationUserId = user.Id;
            }
            else IdentityResults.RequireSuccess(await users.CreateAsync(user, password));
            IdentityResults.RequireSuccess(await users.AddToRoleAsync(user, role));
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
