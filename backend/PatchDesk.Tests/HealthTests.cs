using Microsoft.AspNetCore.Mvc.Testing;
namespace PatchDesk.Tests;

public class HealthTests
{
    [Fact]
    public async Task HealthEndpointResponds()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();
    }
}
