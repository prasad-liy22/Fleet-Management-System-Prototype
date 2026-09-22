using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FleetManagement.Infrastructure.Persistence;

public sealed class FleetManagementDbContextFactory : IDesignTimeDbContextFactory<FleetManagementDbContext>
{
    public FleetManagementDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Set ConnectionStrings__DefaultConnection before running EF commands.");
        var options = new DbContextOptionsBuilder<FleetManagementDbContext>().UseNpgsql(connection).Options;
        return new FleetManagementDbContext(options, new SystemAuditActor(), TimeProvider.System);
    }
}
