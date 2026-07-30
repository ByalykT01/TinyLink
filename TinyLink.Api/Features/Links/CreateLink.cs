using Microsoft.AspNetCore.Http.HttpResults;
using TinyLink.Api.Data;
using TinyLink.Api.Models;
using TinyLink.Api.ShortCodes;

namespace TinyLink.Api.Features.Links;

public static class CreateLink
{
    public sealed record Request(string Url, DateTimeOffset? ExpiresAt);
    public sealed record Responce(string ShortCode, DateTimeOffset? ExpiresAt);

    public static async Task<Results<Created<Responce>, ValidationProblem>> Handle(
                Request request,
                ApplicationDbContext dbContext,
                TimeProvider clock,
                CancellationToken ct)
    {
        var id = new Random().NextInt64(Base62.Domain - 1);
        var code = Base62.Encode(id);

        dbContext.Links.Add(new Link
        {
            Id = id,
            ShortCode = code,
            TargetUrl = request.Url,
            CreatedAt = clock.GetUtcNow(),
            ExpiresAt = request.ExpiresAt ?? DateTimeOffset.UtcNow.AddDays(7)
        });
        await dbContext.SaveChangesAsync(ct);
        return TypedResults.Created($"/api/links/{id}", new Responce(code, request.ExpiresAt));
    }
}
