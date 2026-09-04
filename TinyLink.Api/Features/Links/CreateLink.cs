using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using TinyLink.Api.Data;
using TinyLink.Api.Models;

namespace TinyLink.Api.Features.Links;

internal static class CreateLink
{
    private static readonly TimeSpan _defaultLifetime = TimeSpan.FromDays(7);

    public sealed record Request(string Url, DateTimeOffset? ExpiresAt);
    public sealed record Response(string ShortCode, DateTimeOffset? ExpiresAt, string DeleteToken);

    public static async Task<Results<Created<Response>, ValidationProblem>> Handle(
                Request request,
                UrlPolicy urlPolicy,
                ApplicationDbContext dbContext,
                LinkResolver resolver,
                ShortCodeAllocator codes,
                TimeProvider clock,
                ILoggerFactory loggerFactory,
                CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var errors = new Dictionary<string, string[]>();

        var requestedExpiry = request.ExpiresAt?.ToUniversalTime();

        var urlInput = new UrlInput(request.Url);
        using (var validateActivity = LinkTelemetry.Source.StartActivity("links.validate"))
        {
            if (requestedExpiry is { } expiry && expiry <= now)
            {
                errors["expiresAt"] = ["Must be in the future."];
            }

            ValidationResult urlResult = await urlPolicy.ValidateAsync(urlInput, ct);

            if (!urlResult.IsValid)
            {
                errors["url"] = [.. urlResult.Errors.Select(e => e.ErrorMessage)];
            }

            validateActivity?.SetTag("validation.failed", errors.Count > 0);
        }

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        var expirationTime = requestedExpiry ?? now.Add(_defaultLifetime);

        long id;
        string code;
        using (var allocateActivity = LinkTelemetry.Source.StartActivity("links.allocate"))
        {
            (id, code) = await codes.NextAsync(ct);
        }

        var (deleteTokenValue, deleteTokenHash) = DeleteToken.Generate();

        using (var persistActivity = LinkTelemetry.Source.StartActivity("links.persist"))
        {
            dbContext.Links.Add(new Link
            {
                Id = id,
                ShortCode = code,
                TargetUrl = urlInput.Parsed!,
                CreatedAt = now,
                ExpiresAt = expirationTime,
                DeleteTokenHash = deleteTokenHash
            });

            await dbContext.SaveChangesAsync(ct);
        }

        await resolver.InvalidateAsync(code, ct);

        LinkLog.Created(
            loggerFactory.CreateLogger("TinyLink.Api.Features.Links"),
            code);

        return TypedResults.Created($"/{code}", new Response(code, expirationTime, deleteTokenValue));
    }
}
