using FleetManagement.Application.Access;
using FleetManagement.Application.Common;
using FleetManagement.Application.MasterData;
using FleetManagement.Domain.Entities;
using FleetManagement.Domain.Enums;
using FleetManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Infrastructure.MasterData;

public sealed class VehicleService(FleetManagementDbContext db) : IVehicleService
{
    public async Task<PageDto<VehicleResponse>> ListAsync(MasterQuery query)
    {
        var source = db.Vehicles.AsNoTracking();
        if (query.IncludeDeleted) source = source.IgnoreQueryFilters();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            source = source.Where(x => x.RegistrationNumber.ToLower().Contains(term) || x.Model.ToLower().Contains(term));
        }
        if (!string.IsNullOrEmpty(query.Status))
        {
            var status = MasterValidation.Status<VehicleStatus>(query.Status);
            source = source.Where(x => x.Status == status);
        }
        var total = await source.CountAsync();
        var items = await source.OrderBy(x => x.RegistrationNumber).ThenBy(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();
        return new(items.Select(Map).ToArray(), query.Page, query.PageSize, total);
    }
    public async Task<VehicleResponse> GetAsync(Guid id, bool includeDeleted = false)
    {
        var query = db.Vehicles.AsNoTracking();
        if (includeDeleted) query = query.IgnoreQueryFilters();
        return Map(await query.SingleOrDefaultAsync(x => x.Id == id) ?? throw new RequestException(404, "Vehicle not found."));
    }
    public async Task<VehicleResponse> CreateAsync(VehicleRequest request)
    {
        var entity = new Vehicle();
        Apply(entity, request);
        db.Vehicles.Add(entity);
        await db.SaveChangesAsync();
        return await GetAsync(entity.Id, true);
    }
    public async Task<VehicleResponse> UpdateAsync(Guid id, UpdateVehicleRequest request)
    {
        var entity = await Load(id);
        MasterValidation.Version(entity.Version, request.Version);
        MasterValidation.Editable(entity);
        await GuardTrip(entity);
        Apply(entity, request);
        await db.SaveChangesAsync();
        return await GetAsync(entity.Id, true);
    }
    public async Task<VehicleResponse> ActivateAsync(Guid id, uint version, bool active)
    {
        var entity = await Load(id);
        MasterValidation.Version(entity.Version, version);
        await GuardTrip(entity);
        entity.IsDeleted = !active;
        await db.SaveChangesAsync();
        return await GetAsync(entity.Id, true);
    }
    private async Task<Vehicle> Load(Guid id) => await db.Vehicles.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == id)
        ?? throw new RequestException(404, "Vehicle not found.");
    private async Task GuardTrip(Vehicle entity)
    {
        if (entity.Status == VehicleStatus.OnTrip || await db.Trips.AnyAsync(x => x.VehicleId == entity.Id &&
            (x.Status == TripStatus.Assigned || x.Status == TripStatus.InProgress)))
            throw new RequestException(409, "A vehicle reserved for an active trip cannot be edited or deactivated.");
    }
    private static void Apply(Vehicle entity, VehicleRequest request)
    {
        entity.RegistrationNumber = MasterValidation.Text(request.RegistrationNumber, 32, "Registration number").ToUpperInvariant();
        entity.Model = MasterValidation.Text(request.Model, 120, "Model");
        if (request.Year is < 1900 or > 2100) throw new RequestException(400, "Year must be between 1900 and 2100.");
        if (request.Capacity <= 0 || request.Capacity > 9999999999.99m || decimal.Round(request.Capacity, 2) != request.Capacity)
            throw new RequestException(400, "Capacity must be positive, with at most two decimal places and at most 9999999999.99 kg.");
        if (request.CurrentOdometer < 0 || request.CurrentOdometer > 9007199254740991)
            throw new RequestException(400, "Odometer must be a nonnegative whole number of kilometres within the supported range.");
        if (request.CurrentOdometer < entity.CurrentOdometer) throw new RequestException(409, "Odometer cannot be reduced by ordinary editing.");
        if (MasterValidation.Status<VehicleStatus>(request.Status) != entity.Status)
            throw new RequestException(409, "Vehicle status is controlled by trip and maintenance workflows.");
        entity.Year = request.Year; entity.Capacity = request.Capacity; entity.CurrentOdometer = request.CurrentOdometer;
    }
    private static VehicleResponse Map(Vehicle x) => new(x.Id, x.RegistrationNumber, x.Model, x.Year, x.Capacity,
        x.CurrentOdometer, x.Status.ToString(), x.IsDeleted, x.Version, MasterValidation.Audit(x));
}
