using TinyLink.Api.Extensions;

namespace TinyLink.Api.Features.Links;

public static class LinkEndpoints
{
    public static IEndpointRouteBuilder MapLinkEndpoints(this IEndpointRouteBuilder app)
    {
        var links = app.MapGroup("/api/links").WithTags("Links");

        links.MapPost("/", CreateLink.Handle)
            .RequireRateLimiting(RateLimitingExtensions.CreateLinkPolicy);

        links.MapDelete("/{code:length(7)}", DeleteLink.Handle)
            .WithTags("Delete");

        links.MapGet("/{code:length(7)}", GetLinkStatus.Handle)
            .WithSummary("Report whether a short code is active, gone, or unknown.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status410Gone);

        app.MapGet("/{code:length(7)}", RedirectToTarget.Handle)
            .WithTags("Redirect")
            .WithSummary("Resolve a short code and redirect to its target URL.")
            .WithDescription(
                "Cannot be exercised from this UI: browser fetch follows redirects transparently, "
                + "so you will see the target's response or a CORS error instead of the 302. "
                + "Test with `curl -i` and no -L.")
            .Produces(StatusCodes.Status302Found)
            .Produces(StatusCodes.Status410Gone)
            .Produces<string>(StatusCodes.Status404NotFound, "text/html");


        return app;
    }
}
