using Microsoft.EntityFrameworkCore;
using TinyLink.Api.ShortCodes;

namespace TinyLink.Api.Data;

public sealed class ShortCodeAllocator(ApplicationDbContext dbContext, Cipher cipher)
{
    public async Task<(long Id, string Code)> NextAsync(CancellationToken ct)
    {
        var id = await dbContext.Database
            .SqlQuery<long>($"""SELECT nextval('link_code_req') AS "Value" """)
            .SingleAsync(ct);
        return (id, Base62.Encode(cipher.Permute(id)));
    }
}
