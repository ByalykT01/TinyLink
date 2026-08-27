using FluentAssertions;
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
    [InlineData("not-base64url!")]
    public void Matches_InvalidToken_ReturnsFalse(string? value)
    {
        var (_, Hash) = DeleteToken.Generate();
        DeleteToken.Matches(value, Hash).Should().BeFalse();
    }
}

