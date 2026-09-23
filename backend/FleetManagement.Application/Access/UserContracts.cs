using System.ComponentModel.DataAnnotations;

namespace FleetManagement.Application.Access;

public sealed class FleetRoleAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is string role && FleetRoles.All.Contains(role);
    public override string FormatErrorMessage(string name) => "Choose one of the five supported fleet roles.";
}

public sealed record CreateUserRequest(
    [Required, StringLength(160, MinimumLength = 1)] string DisplayName,
    [Required, EmailAddress, MaxLength(254)] string Email,
    [MaxLength(40)] string? PhoneNumber,
    [Required, FleetRole] string Role,
    Guid? DriverId,
    [Required, MinLength(12), MaxLength(128)] string Password);
public sealed record UpdateUserRequest(
    [Required, StringLength(160, MinimumLength = 1)] string DisplayName,
    [Required, EmailAddress, MaxLength(254)] string Email,
    [MaxLength(40)] string? PhoneNumber,
    [Required, FleetRole] string Role,
    Guid? DriverId,
    bool IsActive,
    [Range(0, int.MaxValue)] int Version);
public sealed record UserAdminDto(
    Guid Id, string DisplayName, string Email, string? PhoneNumber, string Role,
    Guid? DriverId, bool IsActive, int Version, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record DriverOptionDto(Guid Id, string Name, string LicenceNumber);
public sealed record PageDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
public sealed class UserQuery
{
    [Range(1, 1000000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
    [MaxLength(160)] public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public string? Role { get; set; }
}

public interface IUserAdministrationService
{
    Task<PageDto<UserAdminDto>> ListAsync(UserQuery query);
    Task<UserAdminDto> GetAsync(Guid id);
    Task<UserAdminDto> CreateAsync(CreateUserRequest request);
    Task<UserAdminDto> UpdateAsync(Guid id, UpdateUserRequest request);
    Task<IReadOnlyList<DriverOptionDto>> DriverOptionsAsync(Guid? userId, string? search);
}
