using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TinyLink.Api.Data;
using TinyLink.Api.ShortCodes;
using Xunit;

namespace TinyLink.Tests.Integration.Data;

[Collection(PostgresCollectionDefinition.Name)]
public sealed class ShortCodeAllocatorTests(PostgresFixture fixture)
{
    [Fact]
    public async Task NextAsync_ReturnsDistinctSequenceDerivedValues()
    {
        var cipher = new Cipher(new byte[32]);
        await using var dbContext = fixture.CreateDbContext();
        var allocator = new ShortCodeAllocator(dbContext, cipher);

        var first = await allocator.NextAsync(CancellationToken.None);
        var second = await allocator.NextAsync(CancellationToken.None);

        first.Id.Should().NotBe(second.Id);
        second.Id.Should().BeGreaterThan(first.Id);
        first.Code.Should().NotBe(second.Code);
        first.Code.Should().HaveLength(Base62.CodeLength);
        second.Code.Should().HaveLength(Base62.CodeLength);
        first.Code.Should().Be(Base62.Encode(cipher.Permute(first.Id)));
        second.Code.Should().Be(Base62.Encode(cipher.Permute(second.Id)));
        Base62.TryDecode(first.Code, out _).Should().BeTrue();
        Base62.TryDecode(second.Code, out _).Should().BeTrue();
    }
}
