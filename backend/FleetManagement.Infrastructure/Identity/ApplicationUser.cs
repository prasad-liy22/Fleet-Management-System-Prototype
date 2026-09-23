using FleetManagement.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace FleetManagement.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>, IAuditable
{
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int TokenVersion { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
