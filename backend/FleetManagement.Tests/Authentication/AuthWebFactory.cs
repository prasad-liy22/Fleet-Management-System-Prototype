using System.Collections.Concurrent;
using System.Security.Cryptography;
using FleetManagement.Application.Access;
using FleetManagement.Infrastructure.Identity;
using FleetManagement.Infrastructure.Persistence;
using FleetManagement.Tests.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace FleetManagement.Tests.Authentication;

public sealed class CapturedResetDelivery : IPasswordResetDelivery
{
    public bool IsAvailable => true;
    public bool FailDelivery { get; set; }
    public ConcurrentDictionary<string, string> Links { get; } = new();
    public Task DeliverAsync(string recipient, string resetLink, CancellationToken cancellationToken = default)
    {
        if (FailDelivery) throw new IOException("Test delivery failure.");
        Links[recipient] = resetLink;
        return Task.CompletedTask;
    }
}

public sealed class AuthWebFactory(string connection, string environment = "Development", bool captureResets = true, int rateLimit = 10000)
    : WebApplicationFactory<Program>
{
    // Test-only values, unrelated to local demo or deployment credentials.
    public const string TestPassword = "FictionalTest!Password42";
    public string SigningKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    public CapturedResetDelivery Resets { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connection,
            ["Jwt:SigningKey"] = SigningKey,
            ["Jwt:Issuer"] = "FleetManagement",
            ["Jwt:Audience"] = "FleetManagement.Web",
            ["PasswordReset:PublicBaseUrl"] = "https://fleet.example",
            ["Seed:Enabled"] = "false",
            ["Seed:Password"] = TestPassword,
            ["Authentication:RequestsPerMinute"] = rateLimit.ToString(),
            ["Logging:LogLevel:Default"] = "Critical"
        }));
        builder.ConfigureServices(services =>
        {
            if (captureResets)
            {
                services.RemoveAll<IPasswordResetDelivery>();
                services.AddSingleton<IPasswordResetDelivery>(Resets);
            }
            // These probes are compiled into the test assembly only, never the deployed API.
            services.AddControllers().AddApplicationPart(typeof(PolicyProbeController).Assembly);
        });
    }

    public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
    });
}

public sealed class AuthFixture : IAsyncLifetime
{
    public PostgreSqlFixture Database { get; } = new();
    public AuthWebFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FMS_TEST_POSTGRES"))) return;
        await Database.InitializeAsync();
        Factory = new AuthWebFactory(Database.ConnectionString);
        await using var scope = Factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DevelopmentSeeder>().SeedAsync();
        var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedRolesAsync();
        await seeder.SeedDevelopmentUsersAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null) await Factory.DisposeAsync();
        await Database.DisposeAsync();
    }
}

[ApiController, Route("__tests/policy")]
public sealed class PolicyProbeController : ControllerBase
{
    [HttpGet("FleetAdministrator"), Authorize(Policy = FleetPolicies.ManageUsers)]
    public IActionResult Administrator() => NoContent();
    [HttpGet("OperationsCoordinator"), Authorize(Policy = FleetPolicies.ManageOperations)]
    public IActionResult Operations() => NoContent();
    [HttpGet("Mechanic"), Authorize(Policy = FleetPolicies.ManageMaintenance)]
    public IActionResult Mechanic() => NoContent();
    [HttpGet("Driver"), Authorize(Policy = FleetPolicies.DriverWorkflow)]
    public IActionResult Driver() => NoContent();
    [HttpGet("FleetOwner"), Authorize(Policy = FleetPolicies.ReadReports)]
    public IActionResult Owner() => NoContent();
}
