using System.ComponentModel.DataAnnotations;
using System.Text;

namespace FleetManagement.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string Audience { get; set; } = string.Empty;
    [Required] public string SigningKey { get; set; } = string.Empty;
    [Range(5, 60)] public int LifetimeMinutes { get; set; } = 15;
    public bool HasStrongKey() => Encoding.UTF8.GetByteCount(SigningKey) >= 64;
}
