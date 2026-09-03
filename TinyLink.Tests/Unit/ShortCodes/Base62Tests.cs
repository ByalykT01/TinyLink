using FluentAssertions;
using TinyLink.Api.ShortCodes;
using Xunit;

namespace TinyLink.Tests.Unit.ShortCodes;

public class Base62Tests
{
    [Theory]
    [InlineData(0L, "0000000")]
    [InlineData(1L, "0000001")]
    [InlineData(61L, "000000z")]
    [InlineData(62L, "0000010")]
    [InlineData(Base62.Domain - 1, "zzzzzzz")]
    public void Encode_ShouldReturnExpected7CharBase62String(long input, string expected)
    {
        var result = Base62.Encode(input);

        result.Should().HaveLength(7);
        result.Should().Be(expected);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Roundtrip_ShouldEncodeAndDecodeToOriginalValue()
    {
        var originalId = 123_456_789L;

        var code = Base62.Encode(originalId);
        Base62.TryDecode(code, out var decodedId);

        decodedId.Should().Be(originalId);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(61L)]
    [InlineData(62L)]
    [InlineData(123_456_789L)]
    [InlineData(Base62.Domain - 1)]
    public void Roundtrip_ArbitraryValues_ShouldDecodeToOriginalValue(long originalId)
    {
        var code = Base62.Encode(originalId);

        Base62.TryDecode(code, out var decodedId).Should().BeTrue();
        decodedId.Should().Be(originalId);
    }

    [Fact]
    public void Encode_NegativeValue_ShouldThrow()
    {
        var act = () => Base62.Encode(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Encode_ValueAtDomainLimit_ShouldThrow()
    {
        var act = () => Base62.Encode(Base62.Domain);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("invalid!")]
    [InlineData("123456")]
    [InlineData("12345678")]
    [InlineData("       ")]
    [InlineData("🔥123456")]
    public void Decode_InvalidInput_ShouldHandleOrThrow(string invalidCode)
    {
        var success = Base62.TryDecode(invalidCode, out var decodedId);

        success.Should().BeFalse();
        decodedId.Should().Be(0);



    }
}
