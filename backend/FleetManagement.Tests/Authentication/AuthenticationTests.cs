using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FleetManagement.Application.Access;
using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure.Identity;
using FleetManagement.Infrastructure.Persistence;
using FleetManagement.Tests.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace FleetManagement.Tests.Authentication;

public sealed class AuthenticationTests(AuthFixture fixture) : IClassFixture<AuthFixture>
{
    private static async Task<LoginResponse> LoginAsync(HttpClient client, string email, string password = AuthWebFactory.TestPassword)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return login;
    }

    private async Task<HttpClient> SignInAsync(string role = FleetRoles.FleetAdministrator)
    {
        var client = fixture.Factory.Client();
        await LoginAsync(client, IdentitySeeder.DemoEmails[role]);
        return client;
    }

    private static UpdateUserRequest Update(UserAdminDto user) => new(
        user.DisplayName, user.Email, user.PhoneNumber, user.Role, user.DriverId, user.IsActive, user.Version);

    private static CreateUserRequest NewAccount(string role = FleetRoles.FleetOwner, Guid? driverId = null) =>
        new("Fictional test account", $"test-{Guid.NewGuid():N}@fleet.example", null, role, driverId, AuthWebFactory.TestPassword);

    private static async Task<UserAdminDto> CreateAsync(HttpClient admin, CreateUserRequest? request = null)
    {
        using var response = await admin.PostAsJsonAsync("/api/users", request ?? NewAccount());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UserAdminDto>())!;
    }

    [PostgreSqlTheory]
    [InlineData(FleetRoles.FleetAdministrator)]
    [InlineData(FleetRoles.OperationsCoordinator)]
    [InlineData(FleetRoles.Mechanic)]
    [InlineData(FleetRoles.Driver)]
    [InlineData(FleetRoles.FleetOwner)]
    public async Task Each_role_can_login_and_only_access_its_authorized_policy(string role)
    {
        using var client = fixture.Factory.Client();
        var login = await LoginAsync(client, IdentitySeeder.DemoEmails[role]);
        Assert.Equal(role, login.User.Role);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken);
        Assert.Equal(login.User.Id.ToString(), token.Subject);
        Assert.Equal(role, token.Claims.Single(x => x.Type == "role").Value);
        Assert.Equal(login.User.Email, token.Claims.Single(x => x.Type == "email").Value);
        Assert.True(login.ExpiresAt > DateTimeOffset.UtcNow);
        using var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(login.User, await me.Content.ReadFromJsonAsync<CurrentUserDto>());
        if (role == FleetRoles.Driver) Assert.NotNull(login.User.DriverId);

        foreach (var target in FleetRoles.All)
        {
            using var result = await client.GetAsync("/__tests/policy/" + target);
            Assert.Equal(target == role ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden, result.StatusCode);
        }
        using var users = await client.GetAsync("/api/users");
        Assert.Equal(role == FleetRoles.FleetAdministrator ? HttpStatusCode.OK : HttpStatusCode.Forbidden, users.StatusCode);
        if (role != FleetRoles.FleetAdministrator)
        {
            using var write = await client.PostAsJsonAsync("/api/users", NewAccount());
            Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
        }
        var body = await me.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("concurrencyStamp", body, StringComparison.OrdinalIgnoreCase);
    }

    [PostgreSqlFact]
    public async Task Anonymous_requests_cannot_access_me_users_or_role_policies()
    {
        using var client = fixture.Factory.Client();
        foreach (var path in new[] { "/api/auth/me", "/api/users" }.Concat(FleetRoles.All.Select(x => "/__tests/policy/" + x)))
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [PostgreSqlFact]
    public async Task Invalid_password_and_unknown_email_return_same_generic_failure()
    {
        using var client = fixture.Factory.Client();
        using var known = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("owner@fleet.example", "Incorrect!Password42"));
        using var unknown = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("missing@fleet.example", "Incorrect!Password42"));
        Assert.Equal(HttpStatusCode.Unauthorized, known.StatusCode);
        Assert.Equal(known.StatusCode, unknown.StatusCode);
        var a = await known.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        var b = await unknown.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.Equal(a!.Title, b!.Title);
    }

    [PostgreSqlTheory]
    [InlineData("expired")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("signature")]
    public async Task Invalid_JWT_is_rejected(string kind)
    {
        using var client = fixture.Factory.Client();
        var login = await LoginAsync(client, "owner@fleet.example");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken);
        var key = kind == "signature" ? new string('Z', 64) : fixture.Factory.SigningKey;
        var token = new JwtSecurityToken(
            kind == "issuer" ? "WrongIssuer" : "FleetManagement",
            kind == "audience" ? "WrongAudience" : "FleetManagement.Web",
            jwt.Claims.Where(x => x.Type is "sub" or "role" or "email" or "ver"),
            DateTime.UtcNow.AddHours(-1), kind == "expired" ? DateTime.UtcNow.AddMinutes(-1) : DateTime.UtcNow.AddMinutes(5),
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        using var result = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [PostgreSqlFact]
    public async Task Administrator_can_create_view_filter_update_and_audit_accounts()
    {
        using var admin = await SignInAsync();
        var account = await CreateAsync(admin);
        using var detail = await admin.GetAsync("/api/users/" + account.Id);
        Assert.Equal(account, await detail.Content.ReadFromJsonAsync<UserAdminDto>());
        using var updated = await admin.PutAsJsonAsync("/api/users/" + account.Id, Update(account) with { DisplayName = "Updated sample account" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var saved = (await updated.Content.ReadFromJsonAsync<UserAdminDto>())!;
        Assert.Equal("Updated sample account", saved.DisplayName);
        using var stale = await admin.PutAsJsonAsync("/api/users/" + account.Id, Update(account));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using var list = await admin.GetAsync("/api/users?search=" + Uri.EscapeDataString(account.Email) + "&role=FleetOwner&isActive=true&pageSize=10");
        var page = (await list.Content.ReadFromJsonAsync<PageDto<UserAdminDto>>())!;
        Assert.Single(page.Items);
        Assert.Equal(account.Id, page.Items[0].Id);
        var json = await list.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", json, StringComparison.OrdinalIgnoreCase);
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FleetManagementDbContext>();
        var stored = await db.Users.SingleAsync(x => x.Id == account.Id);
        var adminId = await db.Users.Where(x => x.Email == "admin@fleet.example").Select(x => x.Id).SingleAsync();
        Assert.Equal(adminId.ToString(), stored.CreatedBy);
        Assert.Equal(adminId.ToString(), stored.UpdatedBy);
        Assert.NotEqual(AuthWebFactory.TestPassword, stored.PasswordHash);
    }

    [PostgreSqlFact]
    public async Task Deactivation_prevents_login_and_revokes_existing_JWT()
    {
        using var admin = await SignInAsync();
        var account = await CreateAsync(admin);
        using var user = fixture.Factory.Client();
        await LoginAsync(user, account.Email);
        using var updated = await admin.PutAsJsonAsync("/api/users/" + account.Id, Update(account) with { IsActive = false });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using var me = await user.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        using var login = await user.PostAsJsonAsync("/api/auth/login", new LoginRequest(account.Email, AuthWebFactory.TestPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        var inactive = (await updated.Content.ReadFromJsonAsync<UserAdminDto>())!;
        using var activated = await admin.PutAsJsonAsync("/api/users/" + account.Id, Update(inactive) with { IsActive = true });
        Assert.Equal(HttpStatusCode.OK, activated.StatusCode);
        await LoginAsync(user, account.Email);
    }

    [PostgreSqlFact]
    public async Task Role_change_and_logout_invalidate_prior_tokens()
    {
        using var admin = await SignInAsync();
        var account = await CreateAsync(admin);
        using var user = fixture.Factory.Client();
        await LoginAsync(user, account.Email);
        using var changed = await admin.PutAsJsonAsync("/api/users/" + account.Id, Update(account) with { Role = FleetRoles.Mechanic });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        using var stale = await user.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, stale.StatusCode);
        var login = await LoginAsync(user, account.Email);
        Assert.Equal(FleetRoles.Mechanic, login.User.Role);
        using var logout = await user.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using var revoked = await user.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
    }

    [PostgreSqlFact]
    public async Task Driver_linking_requires_valid_unclaimed_non_deleted_driver()
    {
        using var admin = await SignInAsync();
        using var missing = await admin.PostAsJsonAsync("/api/users", NewAccount(FleetRoles.Driver, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        using var claimed = await admin.PostAsJsonAsync("/api/users", NewAccount(FleetRoles.Driver, Guid.Parse("00000000-0000-4000-8000-000000000020")));
        Assert.Equal(HttpStatusCode.Conflict, claimed.StatusCode);
        var driver = new Driver { Name = "Fictional link test", LicenceNumber = "LINK-" + Guid.NewGuid().ToString("N"), Contact = "TEST-NOT-A-PHONE" };
        var deleted = new Driver { Name = "Fictional deleted driver", LicenceNumber = "DELETED-" + Guid.NewGuid().ToString("N"), Contact = "TEST-NOT-A-PHONE", IsDeleted = true };
        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FleetManagementDbContext>();
            db.AddRange(driver, deleted);
            await db.SaveChangesAsync();
        }
        using var deletedResponse = await admin.PostAsJsonAsync("/api/users", NewAccount(FleetRoles.Driver, deleted.Id));
        Assert.Equal(HttpStatusCode.BadRequest, deletedResponse.StatusCode);
        var account = await CreateAsync(admin, NewAccount(FleetRoles.Driver, driver.Id));
        using var user = fixture.Factory.Client();
        var login = await LoginAsync(user, account.Email);
        Assert.Equal(driver.Id, login.User.DriverId);
        using var duplicate = await admin.PostAsJsonAsync("/api/users", NewAccount(FleetRoles.Driver, driver.Id));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [PostgreSqlFact]
    public async Task Invalid_input_and_duplicate_email_are_rejected()
    {
        using var admin = await SignInAsync();
        using var invalid = await admin.PostAsJsonAsync("/api/users", NewAccount() with { Role = "SuperUser", Password = "short" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var duplicate = await admin.PostAsJsonAsync("/api/users", NewAccount() with { Email = "ADMIN@fleet.example" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var invalidPage = await admin.GetAsync("/api/users?page=0");
        Assert.Equal(HttpStatusCode.BadRequest, invalidPage.StatusCode);
    }

    [PostgreSqlFact]
    public async Task Roles_and_development_users_seed_idempotently()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FleetManagementDbContext>();
        var usersBefore = await db.Users.CountAsync();
        var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedRolesAsync();
        await seeder.SeedDevelopmentUsersAsync();
        await seeder.SeedRolesAsync();
        await seeder.SeedDevelopmentUsersAsync();
        Assert.Equal(usersBefore, await db.Users.CountAsync());
        Assert.Equal(FleetRoles.All.OrderBy(x => x), await db.Roles.OrderBy(x => x.Name).Select(x => x.Name).ToArrayAsync());
    }

    [PostgreSqlFact]
    public async Task Reset_is_generic_single_use_and_revokes_existing_sessions()
    {
        using var admin = await SignInAsync();
        var account = await CreateAsync(admin);
        using var client = fixture.Factory.Client();
        await LoginAsync(client, account.Email);
        using var known = await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest(account.Email));
        using var unknown = await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest("absent@fleet.example"));
        Assert.Equal(HttpStatusCode.OK, known.StatusCode);
        Assert.Equal(await known.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
        var body = await known.Content.ReadAsStringAsync();
        Assert.DoesNotContain("token", body, StringComparison.OrdinalIgnoreCase);
        var link = new Uri(fixture.Factory.Resets.Links[account.Email]);
        var token = QueryHelpers.ParseQuery(link.Query)["token"].ToString();
        var reset = new ResetPasswordRequest(account.Email, token, "ChangedFictional!Password77");
        using var result = await client.PostAsJsonAsync("/api/auth/reset-password", reset);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        using var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        using var reused = await client.PostAsJsonAsync("/api/auth/reset-password", reset);
        Assert.Equal(HttpStatusCode.BadRequest, reused.StatusCode);
        await LoginAsync(client, account.Email, reset.Password);
        using var invalid = await client.PostAsJsonAsync("/api/auth/reset-password", reset with { Token = "invalid!" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [PostgreSqlFact]
    public async Task Production_reset_delivery_is_unavailable_uniformly_and_demo_seed_is_forbidden()
    {
        await using var production = new AuthWebFactory(fixture.Database.ConnectionString, "Production", captureResets: false);
        using var client = production.Client();
        foreach (var email in new[] { "owner@fleet.example", "absent@fleet.example" })
        {
            using var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest(email));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
        await using var scope = production.Services.CreateAsyncScope();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedDevelopmentUsersAsync());
    }

    [PostgreSqlFact]
    public async Task Authentication_requests_are_rate_limited()
    {
        await using var limited = new AuthWebFactory(fixture.Database.ConnectionString, rateLimit: 1);
        using var client = limited.Client();
        await LoginAsync(client, "owner@fleet.example");
        using var second = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("owner@fleet.example", AuthWebFactory.TestPassword));
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    [PostgreSqlFact]
    public async Task Concurrent_deactivation_cannot_remove_the_last_active_administrator()
    {
        var isolated = new AuthFixture();
        await isolated.InitializeAsync();
        try
        {
            using var first = isolated.Factory.Client();
            var firstLogin = await LoginAsync(first, "admin@fleet.example");
            var firstAccount = (await first.GetFromJsonAsync<UserAdminDto>("/api/users/" + firstLogin.User.Id))!;
            using var demotion = await first.PutAsJsonAsync("/api/users/" + firstAccount.Id,
                Update(firstAccount) with { Role = FleetRoles.FleetOwner });
            Assert.Equal(HttpStatusCode.Conflict, demotion.StatusCode);
            var secondAccount = await CreateAsync(first, NewAccount(FleetRoles.FleetAdministrator));
            using var second = isolated.Factory.Client();
            await LoginAsync(second, secondAccount.Email);
            var results = await Task.WhenAll(
                first.PutAsJsonAsync("/api/users/" + firstAccount.Id, Update(firstAccount) with { IsActive = false }),
                second.PutAsJsonAsync("/api/users/" + secondAccount.Id, Update(secondAccount) with { IsActive = false }));
            Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
            Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
            foreach (var result in results) result.Dispose();
        }
        finally { await isolated.DisposeAsync(); }
    }

    [PostgreSqlFact]
    public async Task Repeated_password_failures_lock_the_account()
    {
        using var admin = await SignInAsync();
        var account = await CreateAsync(admin);
        using var client = fixture.Factory.Client();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failure = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(account.Email, "Incorrect!Password42"));
            Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode);
        }
        using var locked = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(account.Email, AuthWebFactory.TestPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
    }

    [PostgreSqlFact]
    public async Task Reset_delivery_failure_does_not_reveal_account_existence()
    {
        fixture.Factory.Resets.FailDelivery = true;
        try
        {
            using var client = fixture.Factory.Client();
            using var known = await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest("owner@fleet.example"));
            using var unknown = await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest("missing@fleet.example"));
            Assert.Equal(HttpStatusCode.OK, known.StatusCode);
            Assert.Equal(await known.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
        }
        finally { fixture.Factory.Resets.FailDelivery = false; }
    }}
