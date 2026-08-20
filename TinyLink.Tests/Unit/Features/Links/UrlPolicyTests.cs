using FluentAssertions;
using FluentValidation.Results;
using TinyLink.Api.Features.Links;
using Xunit;

namespace TinyLink.Tests.Unit.Features.Links;

public class UrlPolicyTests
{
    private readonly UrlPolicy _urlPolicy = new();

    [Theory]
    [InlineData("https://example.com", "https://example.com/")]
    [InlineData("http://domain.org/path?query=1", "http://domain.org/path?query=1")]
    [InlineData("HTTPS://UPPERCASE-HOST.COM", "https://uppercase-host.com/")]
    public void Validate_ValidUrls_ShouldBeValidAndReturnNormalizedUrl(string input, string expected)
    {
        var urlInput = new UrlInput(input);

        ValidationResult urlResult = _urlPolicy.Validate(urlInput);

        urlResult.IsValid.Should().BeTrue();
        urlInput.Parsed.Should().Be(new Uri(expected));
        urlResult.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("https://user:password@example.com")]
    [InlineData("javascript:alert(1)")]
    public void Validate_InvalidUrls_ShouldBeInvalid(string input)
    {
        UrlPolicy urlPolicy = new();
        var urlInput = new UrlInput(input);

        ValidationResult urlResult = urlPolicy.Validate(urlInput);

        urlResult.IsValid.Should().BeFalse();
        urlInput.Parsed.Should().NotBeNull();
        urlResult.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("https://exampl")]
    [InlineData("")]
    [InlineData("-1")]
    public void Validate_MalformedUrls_ShouldBeInvalid(string input)
    {
        var urlInput = new UrlInput(input);

        ValidationResult urlResult = _urlPolicy.Validate(urlInput);

        urlResult.IsValid.Should().BeFalse();
        urlInput.Parsed.Should().BeNull();
        urlResult.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void TryNormalize_UrlExceeding2000Chars_ShouldReturnFalse()
    {
        var longUrl = "https://example.com/" + new string('a', UrlPolicy.MaxLength);

        var urlInput = new UrlInput(longUrl);

        ValidationResult urlResult = _urlPolicy.Validate(urlInput);

        // Assert
        urlResult.IsValid.Should().BeFalse();
        urlResult.Errors.Should().NotBeEmpty();
    }
}
