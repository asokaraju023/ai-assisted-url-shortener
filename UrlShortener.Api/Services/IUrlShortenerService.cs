using UrlShortener.Api.Models;

namespace UrlShortener.Api.Services;

public interface IUrlShortenerService
{
    Task<ShortUrl> CreateShortUrlAsync(string originalUrl, CancellationToken cancellationToken);
    Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken);
    Task<bool> RecordClickAsync(string shortCode, CancellationToken cancellationToken);
}
