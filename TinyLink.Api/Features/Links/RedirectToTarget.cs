using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using TinyLink.Api.Data;
using TinyLink.Api.ShortCodes;

namespace TinyLink.Api.Features.Links;

public static class RedirectToTarget
{
    /// <summary>Resolve a short code and redirect to its target URL.</summary>
    /// <remarks>
    /// Cannot be exercised from this UI: browser fetch follows redirects transparently,
    /// so you will see the target's response or a CORS error instead of the 302.
    /// Test with <c>curl -i</c> and no <c>-L</c>.
    /// </remarks>
    /// <param name="code">Seven-character Base62 short code.</param>
    public static async Task<Results<RedirectHttpResult, NotFound, StatusCodeHttpResult>> Handle(
                string code,
                HttpContext http,
                ApplicationDbContext dbContext,
                TimeProvider clock,
                CancellationToken ct)
    {
        if (!Base62.TryDecode(code, out var permuted))
            return NotFound404(http);

        //use cipher there
        var id = permuted;

        var link = await dbContext.Links
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new { l.ShortCode, l.TargetUrl, l.ExpiresAt, l.DeletedAt })
            .FirstOrDefaultAsync(ct);

        if (link is null || link.ShortCode != code)
            return NotFound404(http);

        if (link.DeletedAt is not null || link.ExpiresAt <= clock.GetUtcNow())
        {
            http.Response.Headers.CacheControl = "public, max-age=86400";
            return TypedResults.StatusCode(StatusCodes.Status410Gone);
        }

        http.Response.Headers.CacheControl = "no-store";
        return TypedResults.Redirect(link.TargetUrl, permanent: false, preserveMethod: false);
    }

    private static NotFound NotFound404(HttpContext http)
    {
        http.Response.Headers.CacheControl = "no-store";
        return TypedResults.NotFound();
    }
}

