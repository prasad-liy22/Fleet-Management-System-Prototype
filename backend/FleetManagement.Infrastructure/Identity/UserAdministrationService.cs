using FleetManagement.Application.Access;
using FleetManagement.Application.Common;
using FleetManagement.Domain.Entities;
using FleetManagement.Domain.Enums;
using FleetManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Infrastructure.Identity;

public sealed class UserAdministrationService(FleetManagementDbContext db, UserManager<ApplicationUser> users)
    : IUserAdministrationService
{
    public async Task<PageDto<UserAdminDto>> ListAsync(UserQuery query)
    {
        var source = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            source = source.Where(x => x.DisplayName.ToLower().Contains(search) || x.Email!.ToLower().Contains(search));
        }
        if (query.IsActive is { } active) source = source.Where(x => x.IsActive == active);
        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            ValidateRole(query.Role);
            source = source.Where(x => db.UserRoles.Any(ur => ur.UserId == x.Id &&
                db.Roles.Any(role => role.Id == ur.RoleId && role.Name == query.Role)));
        }
        var total = await source.CountAsync();
        var items = await source.OrderBy(x => x.DisplayName).ThenBy(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();
        var results = new List<UserAdminDto>();
        foreach (var user in items) results.Add(await MapAsync(user));
        return new PageDto<UserAdminDto>(results, query.Page, query.PageSize, total);
    }

    public async Task<UserAdminDto> GetAsync(Guid id) =>
        await MapAsync(await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new RequestException(404, "User not found."));

    public async Task<UserAdminDto> CreateAsync(CreateUserRequest request)
    {
        ValidateRole(request.Role);
        await using var transaction = await db.Database.BeginTransactionAsync();
        await IdentityMutationLock.AcquireAsync(db);
        var driver = await ValidateLinkAsync(null, request.Role, request.DriverId);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), DisplayName = request.DisplayName.Trim(), Email = request.Email.Trim(),
            UserName = request.Email.Trim(), PhoneNumber = request.PhoneNumber?.Trim(), IsActive = true
        };
        IdentityResults.RequireSuccess(await users.CreateAsync(user, request.Password));
        IdentityResults.RequireSuccess(await users.AddToRoleAsync(user, request.Role));
        if (driver is not null) driver.ApplicationUserId = user.Id;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetAsync(user.Id);
    }

    public async Task<UserAdminDto> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        ValidateRole(request.Role);
        await using var transaction = await db.Database.BeginTransactionAsync();
        await IdentityMutationLock.AcquireAsync(db);
        var user = await users.FindByIdAsync(id.ToString()) ?? throw new RequestException(404, "User not found.");
        if (user.TokenVersion != request.Version)
            throw new RequestException(409, "This account changed. Refresh before editing again.");
        var existingRoles = await users.GetRolesAsync(user);
        if (user.IsActive && existingRoles.Contains(FleetRoles.FleetAdministrator) &&
            (!request.IsActive || request.Role != FleetRoles.FleetAdministrator))
        {
            var activeAdmins = await (from member in db.UserRoles
                                      join role in db.Roles on member.RoleId equals role.Id
                                      join account in db.Users on member.UserId equals account.Id
                                      where role.Name == FleetRoles.FleetAdministrator && account.IsActive
                                      select account.Id).CountAsync();
            if (activeAdmins <= 1)
                throw new RequestException(409, "Keep at least one active Fleet Administrator.");
        }
        var driver = await ValidateLinkAsync(id, request.Role, request.DriverId);
        var previousDriver = await db.Drivers.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.ApplicationUserId == id);
        if (previousDriver is not null && previousDriver.Id != driver?.Id)
        {
            if (previousDriver.Status == DriverStatus.OnTrip)
                throw new RequestException(409, "A driver on a trip cannot be unlinked.");
            previousDriver.ApplicationUserId = null;
            // Release the unique link before applying a different driver link.
            await db.SaveChangesAsync();
        }
        if (driver is not null) driver.ApplicationUserId = id;
        if (!string.Equals(user.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            user.EmailConfirmed = false;
        user.DisplayName = request.DisplayName.Trim();
        user.Email = request.Email.Trim();
        user.UserName = request.Email.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        user.IsActive = request.IsActive;
        user.TokenVersion = checked(user.TokenVersion + 1);
        IdentityResults.RequireSuccess(await users.UpdateAsync(user));
        if (existingRoles.Count != 1 || existingRoles[0] != request.Role)
        {
            if (existingRoles.Count > 0)
                IdentityResults.RequireSuccess(await users.RemoveFromRolesAsync(user, existingRoles));
            IdentityResults.RequireSuccess(await users.AddToRoleAsync(user, request.Role));
        }
        // Revoke outstanding reset tokens as well as bearer sessions.
        IdentityResults.RequireSuccess(await users.UpdateSecurityStampAsync(user));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetAsync(user.Id);
    }

    public async Task<IReadOnlyList<DriverOptionDto>> DriverOptionsAsync(Guid? userId, string? search)
    {
        var query = db.Drivers.AsNoTracking().Where(x => x.ApplicationUserId == null || x.ApplicationUserId == userId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x => x.Name.ToLower().Contains(term) || x.LicenceNumber.ToLower().Contains(term));
        }
        return await query.OrderBy(x => x.Name).Take(100)
            .Select(x => new DriverOptionDto(x.Id, x.Name, x.LicenceNumber)).ToListAsync();
    }

    private async Task<Driver?> ValidateLinkAsync(Guid? userId, string role, Guid? driverId)
    {
        if (role != FleetRoles.Driver)
        {
            if (driverId is not null) throw new RequestException(400, "Only Driver accounts may have a driver link.");
            return null;
        }
        if (driverId is null) throw new RequestException(400, "Driver accounts require an existing driver record.");
        var driver = await db.Drivers.SingleOrDefaultAsync(x => x.Id == driverId)
            ?? throw new RequestException(400, "Choose an existing, non-deleted driver.");
        if (driver.ApplicationUserId is not null && driver.ApplicationUserId != userId)
            throw new RequestException(409, "This driver is already linked to another account.");
        if (driver.Status == DriverStatus.OnTrip && driver.ApplicationUserId != userId)
            throw new RequestException(409, "A driver on a trip cannot be linked to a different account.");
        return driver;
    }

    private async Task<UserAdminDto> MapAsync(ApplicationUser user)
    {
        var roles = await users.GetRolesAsync(user);
        var driverId = await db.Drivers.IgnoreQueryFilters().Where(x => x.ApplicationUserId == user.Id)
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync();
        return new UserAdminDto(user.Id, user.DisplayName, user.Email!, user.PhoneNumber,
            roles.SingleOrDefault() ?? "", driverId, user.IsActive, user.TokenVersion, user.CreatedAt, user.UpdatedAt);
    }

    private static void ValidateRole(string role)
    {
        if (!FleetRoles.All.Contains(role)) throw new RequestException(400, "Choose a supported fleet role.");
    }
}
