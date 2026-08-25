using FluentAssertions;
using TinyLink.Api.ShortCodes;
using Xunit;

namespace TinyLink.Tests.Unit.ShortCodes;

public class CipherTests
{
    [Fact]
    public void Permute_IsInjectiveOverALargePrefix()
    {
        var cipher = new Cipher(Convert.FromBase64String("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="));
        const int count = 200_000;
        var seen = new HashSet<long>(count);
        for (long id = 1; id <= count; id++)
            seen.Add(cipher.Permute(id)).Should().BeTrue($"id {id} produced a duplicate code");

    }
}
