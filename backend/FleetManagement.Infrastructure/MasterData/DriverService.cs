using FleetManagement.Application.Access;
using FleetManagement.Application.Common;
using FleetManagement.Application.MasterData;
using FleetManagement.Domain.Entities;
using FleetManagement.Domain.Enums;
using FleetManagement.Infrastructure.Identity;
using FleetManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Infrastructure.MasterData;

public sealed class DriverService(FleetManagementDbContext db) : IDriverService
{
    public async Task<PageDto<DriverResponse>> ListAsync(MasterQuery query)
    {
        var source = db.Drivers.AsNoTracking();
        if (query.IncludeDeleted) source = source.IgnoreQueryFilters();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            source = source.Where(x => x.Name.ToLower().Contains(term) || x.LicenceNumber.ToLower().Contains(term) || x.Contact.Contains(term));
        }
        if (!string.IsNullOrEmpty(query.Status))
        {
            var status = MasterValidation.Status<DriverStatus>(query.Status);
            source = source.Where(x => x.Status == status);
        }
        var total = await source.CountAsync();
        var items = await source.OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();
        var ids = items.Where(x => x.ApplicationUserId != null).Select(x => x.ApplicationUserId!.Value).ToArray();
        var accounts = await db.Users.AsNoTracking().Where(x => ids.Contains(x.Id))
            .Select(x => new LinkedAccountDto(x.Id, x.DisplayName, x.Email!, x.IsActive)).ToDictionaryAsync(x => x.Id);
        return new(items.Select(x => Map(x, x.ApplicationUserId is { } id ? accounts.GetValueOrDefault(id) : null)).ToArray(), query.Page, query.PageSize, total);
    }
    public async Task<DriverResponse> GetAsync(Guid id, bool includeDeleted = false)
    {
        var query = db.Drivers.AsNoTracking();
        if (includeDeleted) query = query.IgnoreQueryFilters();
        var entity = await query.SingleOrDefaultAsync(x => x.Id == id) ?? throw new RequestException(404, "Driver not found.");
        var account = await db.Users.AsNoTracking().Where(x => x.Id == entity.ApplicationUserId)
            .Select(x => new LinkedAccountDto(x.Id, x.DisplayName, x.Email!, x.IsActive)).SingleOrDefaultAsync();
        return Map(entity, account);
    }
    public async Task<DriverResponse> CreateAsync(DriverRequest request)
    {
        var entity = new Driver(); Apply(entity, request);
        db.Drivers.Add(entity); await db.SaveChangesAsync();
        return await GetAsync(entity.Id);
    }
    public async Task<DriverResponse> UpdateAsync(Guid id, UpdateDriverRequest request)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await IdentityMutationLock.AcquireAsync(db);
        var entity = await Load(id);
        MasterValidation.Version(entity.Version, request.Version); MasterValidation.Editable(entity);
        await GuardTrip(entity); Apply(entity, request);
        await db.SaveChangesAsync(); await transaction.CommitAsync();
        return await GetAsync(id);
    }
    public async Task<DriverResponse> ActivateAsync(Guid id, uint version, bool active)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        // Same lock as account linking: an account cannot attach while a driver is being deactivated.
        await IdentityMutationLock.AcquireAsync(db);
        var entity = await Load(id); MasterValidation.Version(entity.Version, version);
        await GuardTrip(entity);
        if (!active && entity.ApplicationUserId != null)
            throw new RequestException(409, "This driver has a linked login account. Reassign or unlink it through Users before deactivating the driver.");
        entity.IsDeleted = !active;
        await db.SaveChangesAsync(); await transaction.CommitAsync();
        return await GetAsync(id, true);
    }
    private async Task<Driver> Load(Guid id) => await db.Drivers.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == id)
        ?? throw new RequestException(404, "Driver not found.");
    private async Task GuardTrip(Driver entity)
    {
        if (entity.Status == DriverStatus.OnTrip || await db.Trips.AnyAsync(x => x.DriverId == entity.Id &&
            (x.Status == TripStatus.Assigned || x.Status == TripStatus.InProgress)))
            throw new RequestException(409, "A driver reserved for an active trip cannot be edited or deactivated.");
    }
    private static void Apply(Driver entity, DriverRequest request)
    {
        entity.Name = MasterValidation.Text(request.Name, 160, "Name");
        entity.LicenceNumber = MasterValidation.Text(request.LicenceNumber, 64, "Licence number").ToUpperInvariant();
        entity.Contact = MasterValidation.Contact(request.Contact, "Contact");
        var status = MasterValidation.Status<DriverStatus>(request.Status);
        if (status == DriverStatus.OnTrip) throw new RequestException(409, "OnTrip status is controlled by the trip workflow.");
        entity.Status = status;
    }
    private static DriverResponse Map(Driver x, LinkedAccountDto? account) => new(x.Id, x.Name, x.LicenceNumber,
        x.Contact, x.Status.ToString(), account, x.IsDeleted, x.Version, MasterValidation.Audit(x));
}
