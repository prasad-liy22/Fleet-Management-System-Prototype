using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FleetManagement.Application.Access;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FleetManagement.Infrastructure.Identity;

public sealed class TokenIssuer(IOptions<JwtOptions> options, TimeProvider clock)
{
    public LoginResponse Issue(ApplicationUser user, CurrentUserDto identity)
    {
        var settings = options.Value;
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(settings.LifetimeMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, identity.Email),
            new Claim("role", identity.Role),
            new Claim("ver", user.TokenVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims,
            now.UtcDateTime, expires.UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                SecurityAlgorithms.HmacSha256));
        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, identity);
    }
}
