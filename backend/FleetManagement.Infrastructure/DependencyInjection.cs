using FleetManagement.Application.Abstractions;
using FleetManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FleetManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<IAuditActor, SystemAuditActor>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddDbContext<FleetManagementDbContext>(options =>
        {
            // Lazy resolution keeps /health as database-independent liveness.
            var connection = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connection))
                throw new InvalidOperationException("Configure ConnectionStrings__DefaultConnection to use persistence.");
            options.UseNpgsql(connection);
        });
        services.AddScoped<DevelopmentSeeder>();
        return services;
    }
}
