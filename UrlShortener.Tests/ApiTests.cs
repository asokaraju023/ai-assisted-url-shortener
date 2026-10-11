using Xunit;
using Microsoft.AspNetCore.Hosting;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UrlShortener.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"urlshortener-{Guid.NewGuid():N}.db");
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={dbPath}");
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_invalid_url_returns_bad_request()
    {
        var response = await _client.PostAsJsonAsync("/api/urls", new { url = "not-a-url" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_then_redirect_and_analytics()
    {
        var create = await _client.PostAsJsonAsync("/api/urls", new { url = "https://example.com/products/1" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var body = await create.Content.ReadFromJsonAsync<CreateResponse>();
        Assert.NotNull(body);

        var redirect = await _client.GetAsync($"/r/{body!.ShortCode}");
        Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);
        Assert.Equal("https://example.com/products/1", redirect.Headers.Location!.ToString());

        var analytics = await _client.GetFromJsonAsync<AnalyticsResponse>($"/api/urls/{body.ShortCode}/analytics");
        Assert.NotNull(analytics);
        Assert.Equal(1, analytics!.ClickCount);
    }

    [Fact]
    public async Task Unknown_code_returns_not_found()
    {
        var response = await _client.GetAsync("/r/doesnotexist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record CreateResponse(string ShortCode, string OriginalUrl, string ShortUrl, DateTime CreatedAtUtc);
    private sealed record AnalyticsResponse(string ShortCode, string OriginalUrl, long ClickCount, DateTime CreatedAtUtc, DateTime? LastAccessedAtUtc);
}
