using Microsoft.AspNetCore.Http.HttpResults;

namespace TinyLink.Api.Features.Links;

internal static class GetLinkStatus
{
    public sealed record Response(Uri TargetUrl, DateTimeOffset? ExpiresAt);

    /// <summary>Report whether a short code is active, gone, or unknown.</summary>
    /// <remarks>
    /// Read-only counterpart to the redirect: same resolution and cache
    /// headers, but JSON instead of a 302 so browsers and the frontend can
    /// render proper 404/410 states instead of navigating blind.
    /// </remarks>
    /// <param name="code">Seven-character Base62 short code.</param>
    public static async Task<Results<Ok<Response>, NotFound, StatusCodeHttpResult>> Handle(
                string code,
                HttpContext http,
                LinkResolver resolver,
                TimeProvider clock,
                CancellationToken ct)
    {
        var link = await resolver.ResolveAsync(code, ct);

        if (link.TargetUrl is null || !link.Exists)
        {
            http.Response.Headers.CacheControl = "no-store";
            return TypedResults.NotFound();
        }

        if (link.DeletedAt is not null || link.ExpiresAt <= clock.GetUtcNow())
        {
            http.Response.Headers.CacheControl = "public, max-age=86400";
            return TypedResults.StatusCode(StatusCodes.Status410Gone);
        }

        http.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(new Response(link.TargetUrl, link.ExpiresAt));
    }
}
