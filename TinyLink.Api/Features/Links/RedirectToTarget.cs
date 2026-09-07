using Microsoft.AspNetCore.Http.HttpResults;

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
    public static async Task<Results<RedirectHttpResult, NotFound, StatusCodeHttpResult, ContentHttpResult>> Handle(
                string code,
                HttpContext http,
                LinkResolver resolver,
                TimeProvider clock,
                IConfiguration configuration,
                CancellationToken ct)
    {

        var link = await resolver.ResolveAsync(code, ct);

        if (link.TargetUrl is null || !link.Exists)
        {
            http.Response.Headers.CacheControl = "no-store";
            if (WantsHtml(http))
            {
                return TypedResults.Content(
                    StatusPages.NotFound(FrontendOrigin(configuration)),
                    "text/html",
                    statusCode: StatusCodes.Status404NotFound);
            }

            return TypedResults.NotFound();
        }

        if (link.DeletedAt is not null || link.ExpiresAt <= clock.GetUtcNow())
        {
            http.Response.Headers.CacheControl = "public, max-age=86400";
            if (WantsHtml(http))
            {
                return TypedResults.Content(
                    StatusPages.Gone(FrontendOrigin(configuration)),
                    "text/html",
                    statusCode: StatusCodes.Status410Gone);
            }

            return TypedResults.StatusCode(StatusCodes.Status410Gone);
        }

        http.Response.Headers.CacheControl = "no-store";
        return TypedResults.Redirect(
            link.TargetUrl.AbsoluteUri,
            permanent: false,
            preserveMethod: false);
    }

    /// <summary>
    /// Browser navigations send <c>Accept: text/html</c>; machines (curl,
    /// fetch) send <c>*/*</c> or JSON. Only the former gets a rendered page.
    /// </summary>
    private static bool WantsHtml(HttpContext http) =>
        http.Request.Headers.Accept.Any(value =>
            value is not null &&
            value.Contains("text/html", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// First configured frontend origin, reused from the CORS setting so the
    /// dead-link page can point back at the app. Null when unconfigured.
    /// </summary>
    private static string? FrontendOrigin(IConfiguration configuration) =>
        configuration.GetSection("Frontend:Origins").Get<string[]>() switch
        {
            { Length: > 0 } origins => origins[0],
            _ => null
        };
}
