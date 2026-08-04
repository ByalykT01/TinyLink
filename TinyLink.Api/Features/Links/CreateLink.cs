using Microsoft.AspNetCore.Http.HttpResults;
using TinyLink.Api.Data;
using TinyLink.Api.Models;
using TinyLink.Api.ShortCodes;

namespace TinyLink.Api.Features.Links;

public static class CreateLink
{

    public sealed record Request(string Url, DateTimeOffset? ExpiresAt);
    public sealed record Response(string ShortCode, DateTimeOffset? ExpiresAt);

    public static async Task<Results<Created<Response>, ValidationProblem>> Handle(
                Request request,
                ApplicationDbContext dbContext,
                TimeProvider clock,
                CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var errors = new Dictionary<string, string[]>();

        if (!UrlPolicy.TryNormalize(request.Url, out var target, out var urlError))
        {
            errors["url"] = [urlError];
            return TypedResults.ValidationProblem(errors);
        }

        if (request.ExpiresAt is { } requested && requested <= now)
            errors["ExpiresAt"] = ["Must be in the future."];

        var expirationTime = request.ExpiresAt ?? clock.GetUtcNow().AddDays(7);

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        var id = new Random().NextInt64(Base62.Domain);
        var code = Base62.Encode(id);

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
