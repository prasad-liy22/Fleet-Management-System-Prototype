using System.ComponentModel.DataAnnotations;
using FleetManagement.Application.Access;

namespace FleetManagement.Application.MasterData;

public sealed class MasterQuery
{
    [Range(1, 1000000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
    [MaxLength(200)] public string? Search { get; set; }
    [MaxLength(24)] public string? Status { get; set; }
    public bool IncludeDeleted { get; set; }
}

public record VehicleRequest(
    [Required, MaxLength(32)] string RegistrationNumber,
    [Required, MaxLength(120)] string Model,
    [Range(1900, 2100)] int Year,
    [Range(typeof(decimal), "0.01", "9999999999.99")] decimal Capacity,
    [Range(typeof(long), "0", "9007199254740991")] long CurrentOdometer,
    [Required] string Status = "Available");
public sealed record UpdateVehicleRequest(
    string RegistrationNumber, string Model, int Year, decimal Capacity, long CurrentOdometer, string Status,
    uint Version) : VehicleRequest(RegistrationNumber, Model, Year, Capacity, CurrentOdometer, Status);
public record DriverRequest(
    [Required, MaxLength(160)] string Name,
    [Required, MaxLength(64)] string LicenceNumber,
    [Required, MaxLength(40)] string Contact,
    [Required] string Status = "Available");
public sealed record UpdateDriverRequest(string Name, string LicenceNumber, string Contact, string Status,
    uint Version) : DriverRequest(Name, LicenceNumber, Contact, Status);
public record CustomerRequest(
    [Required, MaxLength(200)] string CompanyName,
    [MaxLength(160)] string ContactPerson = "",
    [MaxLength(40)] string Telephone = "",
    [MaxLength(254)] string Email = "",
    [MaxLength(500)] string Address = "");
public sealed record UpdateCustomerRequest(string CompanyName, string ContactPerson, string Telephone,
    string Email, string Address, uint Version) : CustomerRequest(CompanyName, ContactPerson, Telephone, Email, Address);
public sealed record ChangeActivationRequest(uint Version);
public sealed record AuditDto(string CreatedBy, DateTimeOffset CreatedAt, string UpdatedBy, DateTimeOffset UpdatedAt);
public sealed record VehicleResponse(Guid Id, string RegistrationNumber, string Model, int Year, decimal Capacity,
    long CurrentOdometer, string Status, bool IsDeleted, uint Version, AuditDto Audit);
public sealed record LinkedAccountDto(Guid Id, string DisplayName, string Email, bool IsActive);
public sealed record DriverResponse(Guid Id, string Name, string LicenceNumber, string Contact, string Status,
    LinkedAccountDto? LinkedAccount, bool IsDeleted, uint Version, AuditDto Audit);
public sealed record CustomerResponse(Guid Id, string CompanyName, string ContactPerson, string Telephone,
    string Email, string Address, bool IsDeleted, uint Version, AuditDto Audit);

public interface IVehicleService
{
    Task<PageDto<VehicleResponse>> ListAsync(MasterQuery query);
    Task<VehicleResponse> GetAsync(Guid id, bool includeDeleted = false);
    Task<VehicleResponse> CreateAsync(VehicleRequest request);
    Task<VehicleResponse> UpdateAsync(Guid id, UpdateVehicleRequest request);
    Task<VehicleResponse> ActivateAsync(Guid id, uint version, bool active);
}
public interface IDriverService
{
    Task<PageDto<DriverResponse>> ListAsync(MasterQuery query);
    Task<DriverResponse> GetAsync(Guid id, bool includeDeleted = false);
    Task<DriverResponse> CreateAsync(DriverRequest request);
    Task<DriverResponse> UpdateAsync(Guid id, UpdateDriverRequest request);
    Task<DriverResponse> ActivateAsync(Guid id, uint version, bool active);
}
public interface ICustomerService
{
    Task<PageDto<CustomerResponse>> ListAsync(MasterQuery query);
    Task<CustomerResponse> GetAsync(Guid id, bool includeDeleted = false);
    Task<CustomerResponse> CreateAsync(CustomerRequest request);
    Task<CustomerResponse> UpdateAsync(Guid id, UpdateCustomerRequest request);
    Task<CustomerResponse> ActivateAsync(Guid id, uint version, bool active);
}
