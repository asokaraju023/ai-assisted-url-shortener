namespace UrlShortener.Api.Models;

public class ShortUrl
{
    public int Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public long ClickCount { get; set; }
    public DateTime? LastAccessedAtUtc { get; set; }
}
