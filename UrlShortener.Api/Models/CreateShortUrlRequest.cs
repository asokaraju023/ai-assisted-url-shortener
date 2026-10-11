namespace UrlShortener.Api.Models;

public sealed class CreateShortUrlRequest
{
    public string Url { get; set; } = string.Empty;
}
