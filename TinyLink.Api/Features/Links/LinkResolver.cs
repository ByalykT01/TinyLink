using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using TinyLink.Api.Data;

namespace TinyLink.Api.Features.Links;

public sealed class LinkResolver(
    HybridCache cache,
    IDbContextFactory<ApplicationDbContext> dbContextFactory)
{
    private const string _cacheKeyPrefix = "links:";

    private static readonly HybridCacheEntryOptions _cacheOptions = new()
    {
        Expiration = TimeSpan.FromSeconds(30),
        LocalCacheExpiration = TimeSpan.FromSeconds(30)

    };

    public async ValueTask<LinkResolution> ResolveAsync(
            string code,
            CancellationToken ct)
    {
        var key = $"{_cacheKeyPrefix}{code}";
        var resolution = await cache.GetOrCreateAsync(
            key,
            (Factory: dbContextFactory, Code: code),
            static async (state, token) =>
            {
                await using var database = await state.Factory.CreateDbContextAsync(token);

                var link = await database.Links
                    .AsNoTracking()
                    .Where(candidate => candidate.ShortCode == state.Code)
                    .Select(candidate => new
                    {
                        candidate.TargetUrl,
                        candidate.ExpiresAt,
                        candidate.DeletedAt
                    })
                    .FirstOrDefaultAsync(token);

                return link is null
                    ? LinkResolution.NotFound
                    : new LinkResolution(
                        true,
                        link.TargetUrl,
                        link.ExpiresAt,
                        link.DeletedAt);
            },
            _cacheOptions,
            cancellationToken: ct);

        if (!resolution.Exists)
        {
            await cache.RemoveAsync(key, ct);
        }

        return resolution;
    }

    public ValueTask InvalidateAsync(
        string code,
        CancellationToken ct) =>
        cache.RemoveAsync(
            $"{_cacheKeyPrefix}{code}", ct);
}
