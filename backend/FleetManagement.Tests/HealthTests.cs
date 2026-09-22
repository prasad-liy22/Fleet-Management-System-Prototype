using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
namespace FleetManagement.Tests;
public class HealthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;
    public HealthTests(WebApplicationFactory<Program> factory) => this.factory = factory;
    [Fact]
    public async Task Application_starts_and_reports_liveness()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
