using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FleetManagement.Application.Access;
using FleetManagement.Domain.Entities;
using FleetManagement.Domain.Enums;
using FleetManagement.Infrastructure.Identity;
using FleetManagement.Tests.Authentication;
using FleetManagement.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FleetManagement.Tests.MasterData;

public sealed class MasterDataTests(AuthFixture fixture) : IClassFixture<AuthFixture>
{
    private async Task<HttpClient> Client(string role = FleetRoles.FleetAdministrator)
    {
        var client = fixture.Factory.Client();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(IdentitySeeder.DemoEmails[role], AuthWebFactory.TestPassword));
        response.EnsureSuccessStatusCode();
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }
    private static Dictionary<string, object?> New(string module, string? prefix = null)
    {
        var key = (prefix ?? "P4") + Guid.NewGuid().ToString("N")[..12];
        return module switch
        {
            "vehicles" => new() { ["registrationNumber"] = key, ["model"] = "Fictional hauler", ["year"] = 2024, ["capacity"] = 1000m, ["currentOdometer"] = 100L, ["status"] = "Available" },
            "drivers" => new() { ["name"] = key, ["licenceNumber"] = key, ["contact"] = "+1 (555) 010-0042", ["status"] = "Available" },
            _ => new() { ["companyName"] = key, ["contactPerson"] = "Sample Contact", ["telephone"] = "+1 555 010 0042", ["email"] = "contact@fictional.example", ["address"] = "Fictional road" }
        };
    }
    private static async Task<JsonElement> Create(HttpClient client, string module, Dictionary<string, object?> request)
    {
        var response = await client.PostAsJsonAsync("/api/" + module, request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
    private static uint Version(JsonElement value) => value.GetProperty("version").GetUInt32();
    private static string Path(string module, JsonElement value) => "/api/" + module + "/" + value.GetProperty("id").GetString();

    [PostgreSqlTheory]
    [InlineData("vehicles", "model")]
    [InlineData("drivers", "name")]
    [InlineData("customers", "companyName")]
    public async Task Full_lifecycle_preserves_rows_audit_and_detects_stale_writes(string module, string field)
    {
        using var client = await Client();
        var request = New(module); var created = await Create(client, module, request); var path = Path(module, created);
        var createdAudit = created.GetProperty("audit");
        Assert.NotEqual("system", createdAudit.GetProperty("createdBy").GetString());
        Assert.True(Guid.TryParse(createdAudit.GetProperty("createdBy").GetString(), out _));
        Assert.True(Version(created) > 0);
        request[field] = "Updated fictional record"; request["version"] = Version(created);
        var updatedResponse = await client.PutAsJsonAsync(path, request);
        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
        var updated = await updatedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(Version(created), Version(updated));
        Assert.Equal(createdAudit.GetProperty("createdAt").GetString(), updated.GetProperty("audit").GetProperty("createdAt").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(path, request)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path + "/deactivate", new { version = Version(created) })).StatusCode);
        var deletion = await client.PostAsJsonAsync(path + "/deactivate", new { version = Version(updated) });
        Assert.Equal(HttpStatusCode.OK, deletion.StatusCode);
        var deleted = await deletion.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(deleted.GetProperty("isDeleted").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path + "?includeDeleted=true")).StatusCode);
        var identity = module == "vehicles" ? "registrationNumber" : module == "drivers" ? "licenceNumber" : "companyName";
        var search = Uri.EscapeDataString((string)request[identity]!);
        var list = await client.GetFromJsonAsync<JsonElement>("/api/" + module + "?search=" + search);
        Assert.Equal(0, list.GetProperty("totalCount").GetInt32());
        var all = await client.GetFromJsonAsync<JsonElement>("/api/" + module + "?includeDeleted=true&search=" + search);
        Assert.Equal(1, all.GetProperty("totalCount").GetInt32());
        request["version"] = Version(deleted);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(path, request)).StatusCode);
        var restore = await client.PostAsJsonAsync(path + "/reactivate", new { version = Version(deleted) });
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
        var restored = await restore.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(restored.GetProperty("isDeleted").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path + "/reactivate", new { version = Version(deleted) })).StatusCode);
        await using var db = fixture.Database.CreateContext();
        var id = created.GetProperty("id").GetGuid();
        Assert.True(module switch {
            "vehicles" => await db.Vehicles.IgnoreQueryFilters().AnyAsync(x => x.Id == id),
            "drivers" => await db.Drivers.IgnoreQueryFilters().AnyAsync(x => x.Id == id),
            _ => await db.CustomerCompanies.IgnoreQueryFilters().AnyAsync(x => x.Id == id)
        });
    }
    [PostgreSqlTheory]
    [InlineData("vehicles", "registrationNumber")]
    [InlineData("drivers", "licenceNumber")]
    public async Task Identifiers_are_normalized_and_reserved_after_deactivation(string module, string field)
    {
        using var client = await Client(); var request = New(module); request[field] = "  " + ((string)request[field]!).ToLowerInvariant() + "  ";
        var first = await Create(client, module, request);
        Assert.Equal(((string)request[field]!).Trim().ToUpperInvariant(), first.GetProperty(field).GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/" + module, request)).StatusCode);
        var deletion = await client.PostAsJsonAsync(Path(module, first) + "/deactivate", new { version = Version(first) });
        Assert.Equal(HttpStatusCode.OK, deletion.StatusCode);
        var conflict = await client.PostAsJsonAsync("/api/" + module, request);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.DoesNotContain("Npgsql", await conflict.Content.ReadAsStringAsync());
    }
    [PostgreSqlTheory]
    [InlineData("vehicles", "capacity", "0", 400)]
    [InlineData("vehicles", "capacity", "1.001", 400)]
    [InlineData("vehicles", "currentOdometer", "-1", 400)]
    [InlineData("vehicles", "year", "1899", 400)]
    [InlineData("vehicles", "model", " ", 400)]
    [InlineData("vehicles", "registrationNumber", " ", 400)]
    [InlineData("vehicles", "status", "OnTrip", 409)]
    [InlineData("vehicles", "status", "UnderMaintenance", 409)]
    [InlineData("drivers", "status", "OnTrip", 409)]
    [InlineData("drivers", "status", "Imaginary", 400)]
    [InlineData("drivers", "name", " ", 400)]
    [InlineData("drivers", "licenceNumber", " ", 400)]
    [InlineData("drivers", "contact", " ", 400)]
    [InlineData("customers", "companyName", " ", 400)]
    [InlineData("customers", "email", "not-an-email", 400)]
    [InlineData("customers", "telephone", "not-a-number", 400)]
    public async Task Backend_rejects_invalid_fields(string module, string field, string value, int status)
    {
        using var client = await Client(); var request = New(module);
        request[field] = field is "capacity" or "currentOdometer" or "year" ? decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture) : value;
        Assert.Equal(status, (int)(await client.PostAsJsonAsync("/api/" + module, request)).StatusCode);
    }
    [PostgreSqlFact]
    public async Task Vehicle_edits_cannot_reduce_odometer_or_change_workflow_status()
    {
        using var client = await Client(); var request = New("vehicles"); var vehicle = await Create(client, "vehicles", request);
        request["version"] = Version(vehicle); request["currentOdometer"] = 99;
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(Path("vehicles", vehicle), request)).StatusCode);
        request["currentOdometer"] = 101; request["status"] = "OnTrip";
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(Path("vehicles", vehicle), request)).StatusCode);
    }
    [PostgreSqlTheory]
    [InlineData("vehicles")][InlineData("drivers")][InlineData("customers")]
    public async Task Lists_search_paginate_and_validate_parameters(string module)
    {
        using var client = await Client(); var prefix = Guid.NewGuid().ToString("N")[..8];
        for (var i = 0; i < 3; i++) await Create(client, module, New(module, prefix));
        var page = await client.GetFromJsonAsync<JsonElement>($"/api/{module}?search={prefix}&page=2&pageSize=2");
        Assert.Equal(3, page.GetProperty("totalCount").GetInt32()); Assert.Equal(1, page.GetProperty("items").GetArrayLength());
        if (module != "customers") {
            var filtered = await client.GetFromJsonAsync<JsonElement>($"/api/{module}?search={prefix}&status=OnTrip");
            Assert.Equal(0, filtered.GetProperty("totalCount").GetInt32());
        }
        foreach (var query in new[] { "page=0", "pageSize=101", "status=invalid" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/{module}?{query}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/{module}/{Guid.NewGuid()}")).StatusCode);
        var missing = New(module); missing["version"] = 1;
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/{module}/{Guid.NewGuid()}", missing)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/{module}/{Guid.NewGuid()}/deactivate", new { version = 1 })).StatusCode);
    }
    [PostgreSqlTheory]
    [InlineData("vehicles")][InlineData("drivers")][InlineData("customers")]
    public async Task Anonymous_and_all_other_roles_cannot_read_or_manage_records(string module)
    {
        foreach (var role in new[] { "", FleetRoles.Driver, FleetRoles.FleetOwner, FleetRoles.Mechanic, FleetRoles.OperationsCoordinator })
        {
            using var client = role == "" ? fixture.Factory.Client() : await Client(role);
            var expected = role == "" ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;
            var path = $"/api/{module}/{Guid.NewGuid()}";
            Assert.Equal(expected, (await client.GetAsync($"/api/{module}?includeDeleted=true")).StatusCode);
            Assert.Equal(expected, (await client.GetAsync(path)).StatusCode);
            Assert.Equal(expected, (await client.PostAsJsonAsync($"/api/{module}", New(module))).StatusCode);
            var edit = New(module); edit["version"] = 1;
            Assert.Equal(expected, (await client.PutAsJsonAsync(path, edit)).StatusCode);
            foreach (var action in new[] { "deactivate", "reactivate" })
                Assert.Equal(expected, (await client.PostAsJsonAsync(path + "/" + action, new { version = 1 })).StatusCode);
        }
    }
    [PostgreSqlFact]
    public async Task Linked_driver_is_displayed_and_cannot_be_deactivated_or_relinked_by_driver_edit()
    {
        using var client = await Client(); var body = New("drivers"); var driver = await Create(client, "drivers", body);
        var id = driver.GetProperty("id").GetGuid();
        var accountResponse = await client.PostAsJsonAsync("/api/users", new CreateUserRequest("Fictional driver", $"{Guid.NewGuid():N}@fleet.example", null, FleetRoles.Driver, id, AuthWebFactory.TestPassword));
        Assert.Equal(HttpStatusCode.Created, accountResponse.StatusCode);
        var account = (await accountResponse.Content.ReadFromJsonAsync<UserAdminDto>())!;
        var linked = await client.GetFromJsonAsync<JsonElement>(Path("drivers", driver));
        Assert.Equal(account.Id, linked.GetProperty("linkedAccount").GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(Path("drivers", driver) + "/deactivate", new { version = Version(linked) })).StatusCode);
        body["version"] = Version(linked); body["applicationUserId"] = Guid.NewGuid(); body["name"] = "Updated linked driver";
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(Path("drivers", driver), body)).StatusCode);
        await using var db = fixture.Database.CreateContext();
        Assert.Equal(account.Id, (await db.Drivers.SingleAsync(x => x.Id == id)).ApplicationUserId);
        Assert.Equal(1, await db.Users.CountAsync(x => x.Id == account.Id));
    }
    [PostgreSqlFact]
    public async Task Active_trip_blocks_resource_edits_even_if_status_is_inconsistent_and_preserves_history()
    {
        using var client = await Client();
        var vr = New("vehicles"); var dr = New("drivers"); var cr = New("customers");
        var vehicle = await Create(client, "vehicles", vr); var driver = await Create(client, "drivers", dr); var customer = await Create(client, "customers", cr);
        await using var db = fixture.Database.CreateContext();
        var order = new Order { CustomerCompanyId = customer.GetProperty("id").GetGuid(), PickupLocation = "Sample A", DeliveryLocation = "Sample B", RequiredDate = new DateOnly(2026, 10, 1), Status = OrderStatus.Assigned };
        var trip = new Trip { Order = order, VehicleId = vehicle.GetProperty("id").GetGuid(), DriverId = driver.GetProperty("id").GetGuid(), Status = TripStatus.Assigned };
        db.Trips.Add(trip); await db.SaveChangesAsync();
        foreach (var (module, item, body) in new[] { ("vehicles", vehicle, vr), ("drivers", driver, dr) }) {
            body["version"] = Version(item);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(Path(module, item), body)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(Path(module, item) + "/deactivate", new { version = Version(item) })).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(Path("customers", customer) + "/deactivate", new { version = Version(customer) })).StatusCode);
        trip.Status = TripStatus.Cancelled; trip.CancellationReason = "Fictional test cancellation"; order.Status = OrderStatus.Cancelled;
        await db.SaveChangesAsync();
        foreach (var (module, item) in new[] { ("vehicles", vehicle), ("drivers", driver), ("customers", customer) })
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(Path(module, item) + "/deactivate", new { version = Version(item) })).StatusCode);
        Assert.True(await db.Trips.IgnoreQueryFilters().AnyAsync(x => x.Id == trip.Id));
        Assert.True(await db.Orders.IgnoreQueryFilters().AnyAsync(x => x.Id == order.Id));
    }
    [PostgreSqlTheory]
    [InlineData("vehicles")][InlineData("drivers")][InlineData("customers")]
    public async Task Two_simultaneous_updates_have_only_one_winner(string module)
    {
        using var client = await Client(); var body = New(module); var item = await Create(client, module, body);
        body["version"] = Version(item);
        body[module == "vehicles" ? "model" : module == "drivers" ? "name" : "companyName"] = "Concurrent update";
        var results = await Task.WhenAll(client.PutAsJsonAsync(Path(module, item), body), client.PutAsJsonAsync(Path(module, item), body));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
    }
}
