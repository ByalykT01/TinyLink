using FluentAssertions;
using TinyLink.Api.ShortCodes;
using Xunit;

namespace TinyLink.Tests.Unit.ShortCodes;

public class CipherTests
{
    private static readonly byte[] _zeroKey = new byte[32];
    private static readonly byte[] _otherKey =
        [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
         0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
         0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
         0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20];

    [Fact]
    public void Permute_IsInjectiveOverALargePrefix()
    {
        var cipher = new Cipher(_zeroKey);
        const int count = 200_000;
        var seen = new HashSet<long>(count);
        for (long id = 1; id <= count; id++)
            seen.Add(cipher.Permute(id)).Should().BeTrue($"id {id} produced a duplicate code");

    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(62L)]
    [InlineData(123_456_789L)]
    [InlineData(Base62.Domain - 2)]
    [InlineData(Base62.Domain - 1)]
    public void Permute_SameKeyAndInput_IsDeterministic(long input)
    {
        var cipher = new Cipher(_zeroKey);

        cipher.Permute(input).Should().Be(cipher.Permute(input));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(62L)]
    [InlineData(123_456_789L)]
    [InlineData(Base62.Domain - 2)]
    [InlineData(Base62.Domain - 1)]
    public void Permute_Output_StaysInsideBase62Domain(long input)
    {
        var cipher = new Cipher(_zeroKey);

        var output = cipher.Permute(input);

        output.Should().BeInRange(0, Base62.Domain - 1);
        Base62.Encode(output).Should().HaveLength(Base62.CodeLength);
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(42L)]
    [InlineData(123_456_789L)]
    public void Permute_DistinctKeys_ProduceDistinctOutputs(long input)
    {
        var first = new Cipher(_zeroKey);
        var second = new Cipher(_otherKey);

        first.Permute(input).Should().NotBe(second.Permute(input));
    }

    [Fact]
    public void Ctor_NullKey_Throws()
    {
        var act = () => new Cipher(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    [InlineData(31)]
    public void Ctor_ShortKey_Throws(int length)
    {
        var act = () => new Cipher(new byte[length]);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Permute_NegativeValue_Throws()
    {
        var cipher = new Cipher(_zeroKey);

        var act = () => cipher.Permute(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Permute_ValueAtDomainLimit_Throws()
    {
        var cipher = new Cipher(_zeroKey);

        var act = () => cipher.Permute(Base62.Domain);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
