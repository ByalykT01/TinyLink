using Microsoft.AspNetCore.Http.HttpResults;
using TinyLink.Api.Data;
using TinyLink.Api.Models;

namespace TinyLink.Api.Features.Links;

public static class CreateLink
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(7);

    public sealed record Request(string Url, DateTimeOffset? ExpiresAt);
    public sealed record Response(string ShortCode, DateTimeOffset? ExpiresAt);

    public static async Task<Results<Created<Response>, ValidationProblem>> Handle(
                Request request,
                ApplicationDbContext dbContext,
                ShortCodeAllocator codes,
                TimeProvider clock,
                CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var errors = new Dictionary<string, string[]>();

        var requestedExpiry = request.ExpiresAt?.ToUniversalTime();


        if (requestedExpiry is { } expiry && expiry <= now)
            errors["expiresAt"] = ["Must be in the future."];

        if (!UrlPolicy.TryNormalize(request.Url, out var target, out var urlError))
        {
            errors["url"] = [urlError];
        }

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        var expirationTime = requestedExpiry ?? now.Add(DefaultLifetime);

        var (id, code) = await codes.NextAsync(ct);

        dbContext.Links.Add(new Link
        {
            Id = id,
            ShortCode = code,
            TargetUrl = target,
            CreatedAt = clock.GetUtcNow(),
            ExpiresAt = expirationTime,
        });

        await dbContext.SaveChangesAsync(ct);
        return TypedResults.Created($"/{code}", new Response(code, expirationTime));
    }
}
