using System.Net;
using FluentAssertions;

namespace Tungsten.Api.Tests.Integration;

public class HealthCheckTests(TestWebApplicationFactory factory) : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task HealthLive_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health/live");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task HealthReady_ReturnsStatus()
    {
        var response = await _client.GetAsync("/health/ready");
        // May be 200 or 503 depending on migration state
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }
}
