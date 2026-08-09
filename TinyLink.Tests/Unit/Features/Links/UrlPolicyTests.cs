using FluentAssertions;
using TinyLink.Api.Features.Links;

namespace TinyLink.Tests.Unit.Features.Links;

public class UrlPolicyTests
{
    [Theory]
    [InlineData("https://example.com", "https://example.com/")]
    [InlineData("http://domain.org/path?query=1", "http://domain.org/path?query=1")]
    [InlineData("HTTPS://UPPERCASE-HOST.COM", "https://uppercase-host.com/")]

    public void TryNormalize_ValidUrls_ShouldReturnTrueAndNormalizedUrl(string input, string expected)
    {
        bool isValid = UrlPolicy.TryNormalize(input, out var normalized, out var error);

        isValid.Should().BeTrue();
        normalized.Should().Be(expected);
        error.Should().BeNull();
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("https://user:password@example.com")]
    [InlineData("not-a-url")]
    [InlineData("javascript:alert(1)")]
    [InlineData("")]
    public void TryNormalize_InvalidUrls_ShouldReturnFalse(string invalidUrl)
    {
        // Act
        bool isValid = UrlPolicy.TryNormalize(invalidUrl, out var normalizedUrl, out var error);

        // Assert
        isValid.Should().BeFalse();
        normalizedUrl.Should().BeNull();
        error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void TryNormalize_UrlExceeding2000Chars_ShouldReturnFalse()
    {
        // Arrange
        var longUrl = "https://example.com/" + new string('a', UrlPolicy.MaxLength);

        // Act
        bool isValid = UrlPolicy.TryNormalize(longUrl, out _, out var error);

        // Assert
        isValid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();

    }
}
