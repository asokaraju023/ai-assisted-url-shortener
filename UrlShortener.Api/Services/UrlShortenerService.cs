using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Services;

public sealed class UrlShortenerService(UrlShortenerDbContext db) : IUrlShortenerService
{
    private const string Characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int CodeLength = 7;

    public async Task<ShortUrl> CreateShortUrlAsync(string originalUrl, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Only valid absolute HTTP/HTTPS URLs are supported.");
        }

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var entity = new ShortUrl
            {
                OriginalUrl = uri.AbsoluteUri,
                ShortCode = GenerateShortCode(CodeLength),
                CreatedAtUtc = DateTime.UtcNow
            };

            db.ShortUrls.Add(entity);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return entity;
            }
            catch (DbUpdateException) when (attempt < 4)
            {
                db.Entry(entity).State = EntityState.Detached;
            }
        }

        throw new InvalidOperationException("Could not generate a unique short code.");
    }

    public Task<ShortUrl?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken) =>
        db.ShortUrls.AsNoTracking().FirstOrDefaultAsync(x => x.ShortCode == shortCode, cancellationToken);

    public async Task<bool> RecordClickAsync(string shortCode, CancellationToken cancellationToken)
    {
        var url = await db.ShortUrls
            .FirstOrDefaultAsync(
                x => x.ShortCode == shortCode,
                cancellationToken);

        if (url is null)
        {
            return false;
        }

        url.ClickCount++;
        url.LastAccessedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static string GenerateShortCode(int length)
    {
        Span<char> result = stackalloc char[length];
        for (var i = 0; i < length; i++)
            result[i] = Characters[RandomNumberGenerator.GetInt32(Characters.Length)];
        return new string(result);
    }
}
