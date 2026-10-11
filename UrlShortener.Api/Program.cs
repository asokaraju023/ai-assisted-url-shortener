using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using UrlShortener.Api.Data;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<UrlShortenerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=urlshortener.db"));

builder.Services.AddScoped<IUrlShortenerService, UrlShortenerService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("create", limiterOptions =>
    {
        limiterOptions.PermitLimit = 30;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UrlShortenerDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseRateLimiter();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapPost("/api/requirements/analyze", (RequirementAnalysisRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Requirement))
        return Results.BadRequest(new { error = "Requirement is required." });

    var requirement = request.Requirement.Trim();
    var isUrlShortener = requirement.Contains("url shortener", StringComparison.OrdinalIgnoreCase)
        || requirement.Contains("short URL", StringComparison.OrdinalIgnoreCase);

    if (!isUrlShortener)
    {
        return Results.Ok(new RequirementAnalysisResponse(
            requirement,
            "Convert the business statement into a testable engineering scope before implementation.",
            ["Clarify functional and non-functional requirements before coding."],
            ["Scale, security, availability, data retention and acceptance criteria are unspecified."],
            ["Clarify requirements", "Define API contract", "Define data model", "Implement", "Test", "Validate security/performance", "Document trade-offs"],
            ["Generate candidate task breakdown", "Suggest edge cases and tests", "Review code and documentation"],
            ["Requirement-to-test traceability", "Unit and integration tests", "Code review", "Security and performance review"],
            ["Ambiguous requirements", "Incorrect AI suggestions", "Unvalidated assumptions"]));
    }

    return Results.Ok(new RequirementAnalysisResponse(
        requirement,
        "Provide a scalable service that maps short codes to destination URLs, persists mappings, redirects clients, and exposes aggregate usage analytics.",
        ["Prototype uses ASP.NET Core 8 and SQLite.", "Only HTTP/HTTPS destinations are accepted.", "Aggregate analytics are sufficient for the prototype."],
        ["Peak RPS and traffic ratio", "Availability/RTO/RPO", "Analytics retention and privacy", "Authentication and abuse controls", "Expiry/custom aliases", "Multi-region requirements"],
        ["Clarify requirements and acceptance criteria", "Define API contracts", "Design persistence and indexes", "Implement URL validation and code generation", "Implement redirect path", "Implement analytics", "Add unit and integration tests", "Review security/performance", "Document deployment and trade-offs"],
        ["Generate and refine API design", "Generate implementation scaffolding", "Generate unit/integration tests", "Review code for security, concurrency and maintainability", "Draft documentation and test cases"],
        ["Build and test", "Validate invalid URLs and unknown codes", "Verify redirect behavior", "Verify click-count accuracy", "Review collision handling", "Review rate limiting and abuse risks", "Confirm assumptions with stakeholders"],
        ["Code collisions", "Database bottleneck", "Analytics write contention", "Abuse/spam", "AI-generated defects", "Requirement ambiguity"]));
});

app.MapPost("/api/urls", async (
    CreateShortUrlRequest request,
    IUrlShortenerService service,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Url))
        return Results.BadRequest(new { error = "URL is required." });

    try
    {
        var result = await service.CreateShortUrlAsync(request.Url.Trim(), cancellationToken);
        var shortUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/r/{result.ShortCode}";
        return Results.Created($"/api/urls/{result.ShortCode}", new
        {
            result.ShortCode,
            result.OriginalUrl,
            ShortUrl = shortUrl,
            result.CreatedAtUtc
        });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireRateLimiting("create");


app.MapGet("/r/{shortCode}", async (
    string shortCode,
    IUrlShortenerService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.GetByShortCodeAsync(shortCode, cancellationToken);

    if (result is null)
    {
        return Results.NotFound(new
        {
            error = "Short URL not found.",
            requestedShortCode = shortCode
        });
    }

    var recorded = await service.RecordClickAsync(shortCode, cancellationToken);

    if (!recorded)
    {
        return Results.NotFound(new
        {
            error = "Short URL could not be recorded.",
            requestedShortCode = shortCode
        });
    }

    return Results.Redirect(result.OriginalUrl);
});


app.MapGet("/api/urls/{shortCode}/analytics", async (
    string shortCode,
    IUrlShortenerService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.GetByShortCodeAsync(shortCode, cancellationToken);
    if (result is null) return Results.NotFound(new { error = "Short URL not found." });

    return Results.Ok(new
    {
        result.ShortCode,
        result.OriginalUrl,
        result.ClickCount,
        result.CreatedAtUtc,
        result.LastAccessedAtUtc
    });
});

app.Run();

public partial class Program { }
