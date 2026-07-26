using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TinyLink.Api.Services;

namespace TinyLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ShortenController(ShortenService shortenService)
{

    [HttpPost("strtoint")]
    public Created<TryIfWorksReturnLong> StringToInt(TryIfWorksEntry entry)
    {
        var ret = new TryIfWorksReturnLong(shortenService.CodeToEncodedId(entry.Entry));
        return TypedResults.Created("1", ret);
    }

    [HttpPost("inttostr")]
    public Created<TryIfWorksReturn> IntToString(TryIfWorksEntryLong entry)
    {
        var ret = new TryIfWorksReturn(shortenService.EncodedIdToCode(entry.Entry));
        return TypedResults.Created("1", ret);
    }


    public record TryIfWorksReturn(string Result);
    public record TryIfWorksReturnLong(long Result);
    public record TryIfWorksEntry(string Entry);
    public record TryIfWorksEntryLong(long Entry);
}


