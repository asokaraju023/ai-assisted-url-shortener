using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Data;

public sealed class UrlShortenerDbContext(DbContextOptions<UrlShortenerDbContext> options) : DbContext(options)
{
    public DbSet<ShortUrl> ShortUrls => Set<ShortUrl>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ShortUrl>();
        entity.HasIndex(x => x.ShortCode).IsUnique();
        entity.Property(x => x.OriginalUrl).IsRequired();
        entity.Property(x => x.ShortCode).IsRequired();
    }
}
