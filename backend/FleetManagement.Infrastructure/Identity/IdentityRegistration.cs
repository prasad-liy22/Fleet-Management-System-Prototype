using System.Text;
using FleetManagement.Application.Access;
using FleetManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FleetManagement.Infrastructure.Identity;

public static class IdentityRegistration
{
    public static IServiceCollection AddFleetIdentity(this IServiceCollection services, IConfiguration config, IHostEnvironment environment)
    {
        services.AddOptions<JwtOptions>().Bind(config.GetSection(JwtOptions.Section))
            .ValidateDataAnnotations().Validate(x => x.HasStrongKey(), "JWT signing key must contain at least 64 UTF-8 bytes.")
            .ValidateOnStart();
        services.AddOptions<ResetOptions>().Bind(config.GetSection("PasswordReset"))
            .Validate(x => Uri.TryCreate(x.PublicBaseUrl, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttps || environment.IsDevelopment() && uri.IsLoopback && uri.Scheme == Uri.UriSchemeHttp),
                "PasswordReset:PublicBaseUrl must be HTTPS (loopback HTTP allowed in Development).").ValidateOnStart();

        var protection = services.AddDataProtection().SetApplicationName("FleetManagement");
        if (config["DataProtection:KeyDirectory"] is { Length: > 0 } keys)
            protection.PersistKeysToFileSystem(new DirectoryInfo(keys));

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;
        }).AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<FleetManagementDbContext>()
            .AddSignInManager().AddDefaultTokenProviders();
        services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromMinutes(30));
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwt) =>
            {
                var settings = jwt.Value;
                options.MapInboundClaims = false;
                options.IncludeErrorDetails = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = settings.Issuer,
                    ValidateAudience = true, ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                    ValidateLifetime = true, RequireExpirationTime = true, RequireSignedTokens = true,
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    ClockSkew = TimeSpan.Zero, NameClaimType = "sub", RoleClaimType = "role"
                };
                options.EventsType = typeof(FleetJwtEvents);
            });
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            foreach (var (policy, role) in FleetPolicies.Roles)
                options.AddPolicy(policy, p => p.RequireAuthenticatedUser().RequireRole(role));
        });
        services.AddScoped<FleetJwtEvents>();
        services.AddScoped<TokenIssuer>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserAdministrationService, UserAdministrationService>();
        services.AddScoped<IdentitySeeder>();
        services.AddSingleton<IPasswordResetDelivery>(_ =>
        {
            var directory = config["PasswordReset:DevelopmentPickupDirectory"];
            return environment.IsDevelopment() && !string.IsNullOrWhiteSpace(directory)
                ? new DevelopmentPasswordResetDelivery(Path.GetFullPath(directory))
                : new UnavailablePasswordResetDelivery();
        });
        return services;
    }

    private sealed class ResetOptions
    {
        public string PublicBaseUrl { get; set; } = string.Empty;
    }
}
