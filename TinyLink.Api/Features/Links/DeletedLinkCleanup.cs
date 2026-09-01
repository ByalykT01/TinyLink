using Microsoft.EntityFrameworkCore;
using TinyLink.Api.Data;

namespace TinyLink.Api.Features.Links;

internal sealed class DeletedLinkCleanup(
            IDbContextFactory<ApplicationDbContext> dbContextFactory,
            TimeProvider clock)
{
    public async Task<int> ExecuteAsync(
            TimeSpan retention,
            CancellationToken ct)
    {
        var cutoff = clock.GetUtcNow() - retention;

        await using var database = await dbContextFactory.CreateDbContextAsync(ct);

        return await database.Links
            .Where(link =>
                  (link.DeletedAt != null && link.DeletedAt <= cutoff) ||
                  (link.ExpiresAt != null && link.ExpiresAt <= cutoff))
            .ExecuteDeleteAsync(ct);
    }
}
