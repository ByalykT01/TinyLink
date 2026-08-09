using FluentAssertions;
using TinyLink.Api.ShortCodes;

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
        long originalId = 123_456_789L;

        string code = Base62.Encode(originalId);
        Base62.TryDecode(code, out var decodedId);

        decodedId.Should().Be(originalId);
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
        bool success = Base62.TryDecode(invalidCode, out var decodedId);

        success.Should().BeFalse();
        decodedId.Should().Be(0);



    }
}
