using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using TinyLink.Api.Features.Links;
using Xunit;
namespace TinyLink.Tests.Unit.Features.Links;

public sealed class DeleteTokenTests
{
    [Fact]
    public void Generate_ReturnsVerifiableToken()
    {
        var (Value, Hash) = DeleteToken.Generate();
        Value.Should().NotBeNullOrWhiteSpace();
        Hash.Should().HaveCount(DeleteToken.HashSizeInBytes);
        DeleteToken.Matches(Value, Hash).Should().BeTrue();
    }
    [Fact]
    public void Generate_ReturnsUniqueTokens()
    {
        var first = DeleteToken.Generate();
        var second = DeleteToken.Generate();

        first.Value.Should().NotBe(second.Value);
        first.Hash.Should().NotEqual(second.Hash);
    }
    [Fact]
    public void Matches_ModifiedHash_ReturnsFalse()
    {
        var (Value, Hash) = DeleteToken.Generate();
        var modifiedHash = Hash.ToArray();
        modifiedHash[0] ^= 0xFF;
        DeleteToken.Matches(Value, modifiedHash).Should().BeFalse();
    }
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-base64url!")]
    public void Matches_InvalidToken_ReturnsFalse(string? value)
    {
        var (_, Hash) = DeleteToken.Generate();
        DeleteToken.Matches(value, Hash).Should().BeFalse();
    }
    [Fact]
    public void Matches_TokenDecodingToWrongLength_ReturnsFalse()
    {
        var (_, Hash) = DeleteToken.Generate();
        var shortToken = WebEncoders.Base64UrlEncode(new byte[16]);

        DeleteToken.Matches(shortToken, Hash).Should().BeFalse();
    }
    [Fact]
    public void Matches_NullExpectedHash_ReturnsFalse()
    {
        var (Value, _) = DeleteToken.Generate();

        DeleteToken.Matches(Value, null).Should().BeFalse();
    }
    [Fact]
    public void Matches_ShortExpectedHash_ReturnsFalse()
    {
        var (Value, _) = DeleteToken.Generate();

        DeleteToken.Matches(Value, new byte[16]).Should().BeFalse();
    }
}
