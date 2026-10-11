using Xunit;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Services;

namespace UrlShortener.Tests;

public class UrlShortenerServiceTests
{
    private static UrlShortenerDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<UrlShortenerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new UrlShortenerDbContext(options);
    }

    [Fact]
    public async Task Create_rejects_non_http_url()
    {
        await using var db = CreateDb();
        var service = new UrlShortenerService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateShortUrlAsync("ftp://example.com", CancellationToken.None));
    }

    [Fact]
    public async Task Create_persists_mapping()
    {
        await using var db = CreateDb();
        var service = new UrlShortenerService(db);

        var result = await service.CreateShortUrlAsync("https://example.com/a", CancellationToken.None);
        var stored = await service.GetByShortCodeAsync(result.ShortCode, CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal("https://example.com/a", stored!.OriginalUrl);
        Assert.Equal(0, stored.ClickCount);
    }

    [Fact]
    public async Task Record_click_updates_analytics()
    {
        await using var db = CreateDb();
        var service = new UrlShortenerService(db);
        var result = await service.CreateShortUrlAsync("https://example.com", CancellationToken.None);

        Assert.True(await service.RecordClickAsync(result.ShortCode, CancellationToken.None));
        var stored = await service.GetByShortCodeAsync(result.ShortCode, CancellationToken.None);

        Assert.Equal(1, stored!.ClickCount);
        Assert.NotNull(stored.LastAccessedAtUtc);
    }
}
