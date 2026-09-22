using FleetManagement.Application.Abstractions;
using FleetManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Xunit;

namespace FleetManagement.Tests.Persistence;

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FMS_TEST_POSTGRES")))
            Skip = "Set FMS_TEST_POSTGRES to run real PostgreSQL tests (requires CREATE DATABASE).";
    }
}

public sealed class PostgreSqlTheoryAttribute : TheoryAttribute
{
    public PostgreSqlTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FMS_TEST_POSTGRES")))
            Skip = "Set FMS_TEST_POSTGRES to run real PostgreSQL tests (requires CREATE DATABASE).";
    }
}

/// <summary>Creates and drops only its own randomly named test database; never resets the supplied database.</summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly string databaseName = $"fms_test_{Guid.NewGuid():N}";
    private string? adminConnection;
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("FMS_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(configured))
            return;
        var builder = new NpgsqlConnectionStringBuilder(configured) { Pooling = false };
        adminConnection = builder.ConnectionString;
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        await command.ExecuteNonQueryAsync();
        builder.Database = databaseName;
        ConnectionString = builder.ConnectionString;
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public FleetManagementDbContext CreateContext(string actor = "test:operator", TimeProvider? clock = null) =>
        new(new DbContextOptionsBuilder<FleetManagementDbContext>().UseNpgsql(ConnectionString).Options,
            new TestAuditActor(actor), clock ?? TimeProvider.System);

    public async Task DisposeAsync()
    {
        if (adminConnection is null)
            return;
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)";
        await command.ExecuteNonQueryAsync();
    }
}

internal sealed record TestAuditActor(string ActorId) : IAuditActor;

internal sealed class TestClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal sealed class TestEnvironment(string name) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = name;
    public string ApplicationName { get; set; } = "FleetManagement.Tests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
