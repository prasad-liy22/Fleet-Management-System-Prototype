using FleetManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Infrastructure.Identity;

internal static class IdentityMutationLock
{
    // Call inside a database transaction. Serializes role/link/last-admin and revocation changes.
    public static Task AcquireAsync(FleetManagementDbContext db) =>
        db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7462012302)");
}
