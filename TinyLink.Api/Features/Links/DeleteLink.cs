using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TinyLink.Api.Data;

namespace TinyLink.Api.Features.Links;

internal static class DeleteLink
{
    public static async Task<Results<NoContent, NotFound>> Handle(
            string code,
            [FromHeader(Name = "Authorization")] string? authorization,
            ApplicationDbContext dbContext,
            LinkResolver resolver,
            TimeProvider clock,
            ILoggerFactory loggerFactory,
            CancellationToken ct
            )
    {
        bool authorized;
        long linkId;
        using (var authorizeActivity = LinkTelemetry.Source.StartActivity("links.authorize"))
        {
            if (!AuthenticationHeaderValue.TryParse(
                            authorization,
                            out var credentials) ||
                        credentials is null ||
                        !credentials.Scheme.Equals(
                            "Bearer",
                            StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrWhiteSpace(credentials.Parameter))
            {
                authorized = false;
                linkId = 0;
            }
            else
            {
                var foundEntry = await dbContext.Links
                    .Where(l => l.ShortCode == code)
                    .Select(l => new
                    {
                        l.Id,
                        l.DeleteTokenHash,
                        l.DeletedAt,
                    }).SingleOrDefaultAsync(ct);

                if (foundEntry?.DeleteTokenHash is { } expectedHash &&
                        DeleteToken.Matches(credentials.Parameter, expectedHash))
                {
                    authorized = true;
                    linkId = foundEntry.Id;
                }
                else
                {
                    authorized = false;
                    linkId = 0;
                }
            }

            authorizeActivity?.SetTag("auth.succeeded", authorized);
        }

        if (!authorized)
        {
            return TypedResults.NotFound();
        }

        var now = clock.GetUtcNow();

        using (var deleteActivity = LinkTelemetry.Source.StartActivity("links.soft-delete"))
        {
            await dbContext.Links
                .Where(l => l.Id == linkId && l.DeletedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.DeletedAt, now), ct);
        }

        await resolver.InvalidateAsync(code, ct);

        LinkLog.Deleted(
            loggerFactory.CreateLogger("TinyLink.Api.Features.Links"),
            code);

        return TypedResults.NoContent();
    }
}
