using FleetManagement.Application.Access;
using FleetManagement.Application.Common;
using FleetManagement.Application.MasterData;
using FleetManagement.Domain.Entities;
using FleetManagement.Domain.Enums;
using FleetManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Infrastructure.MasterData;

public sealed class CustomerService(FleetManagementDbContext db) : ICustomerService
{
    public async Task<PageDto<CustomerResponse>> ListAsync(MasterQuery query)
    {
        if (!string.IsNullOrEmpty(query.Status)) throw new RequestException(400, "Customers do not have an operational status filter.");
        var source = db.CustomerCompanies.AsNoTracking();
        if (query.IncludeDeleted) source = source.IgnoreQueryFilters();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            source = source.Where(x => x.CompanyName.ToLower().Contains(term) || x.ContactPerson.ToLower().Contains(term) ||
                x.Email.ToLower().Contains(term) || x.Telephone.Contains(term));
        }
        var total = await source.CountAsync();
        var items = await source.OrderBy(x => x.CompanyName).ThenBy(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();
        return new(items.Select(Map).ToArray(), query.Page, query.PageSize, total);
    }
    public async Task<CustomerResponse> GetAsync(Guid id, bool includeDeleted = false)
    {
        var query = db.CustomerCompanies.AsNoTracking();
        if (includeDeleted) query = query.IgnoreQueryFilters();
        return Map(await query.SingleOrDefaultAsync(x => x.Id == id) ?? throw new RequestException(404, "Customer not found."));
    }
    public async Task<CustomerResponse> CreateAsync(CustomerRequest request)
    {
        var entity = new CustomerCompany(); Apply(entity, request);
        db.CustomerCompanies.Add(entity); await db.SaveChangesAsync(); return await GetAsync(entity.Id, true);
    }
    public async Task<CustomerResponse> UpdateAsync(Guid id, UpdateCustomerRequest request)
    {
        var entity = await Load(id); MasterValidation.Version(entity.Version, request.Version);
        MasterValidation.Editable(entity); Apply(entity, request);
        await db.SaveChangesAsync(); return await GetAsync(entity.Id, true);
    }
    public async Task<CustomerResponse> ActivateAsync(Guid id, uint version, bool active)
    {
        var entity = await Load(id); MasterValidation.Version(entity.Version, version);
        if (!active && await db.Orders.AnyAsync(x => x.CustomerCompanyId == id &&
            x.Status != OrderStatus.Completed && x.Status != OrderStatus.Cancelled))
            throw new RequestException(409, "A customer with unfinished orders cannot be deactivated.");
        entity.IsDeleted = !active; await db.SaveChangesAsync(); return await GetAsync(entity.Id, true);
    }
    private async Task<CustomerCompany> Load(Guid id) => await db.CustomerCompanies.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == id)
        ?? throw new RequestException(404, "Customer not found.");
    private static void Apply(CustomerCompany entity, CustomerRequest request)
    {
        entity.CompanyName = MasterValidation.Text(request.CompanyName, 200, "Company name");
        entity.ContactPerson = MasterValidation.Text(request.ContactPerson, 160, "Contact person", false);
        entity.Telephone = MasterValidation.Contact(request.Telephone, "Telephone", false);
        entity.Email = MasterValidation.Text(request.Email, 254, "Email", false); MasterValidation.Email(entity.Email);
        entity.Address = MasterValidation.Text(request.Address, 500, "Address", false);
    }
    private static CustomerResponse Map(CustomerCompany x) => new(x.Id, x.CompanyName, x.ContactPerson, x.Telephone,
        x.Email, x.Address, x.IsDeleted, x.Version, MasterValidation.Audit(x));
}
