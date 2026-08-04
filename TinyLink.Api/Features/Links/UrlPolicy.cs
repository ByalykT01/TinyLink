using System.Diagnostics.CodeAnalysis;

namespace TinyLink.Api.Features.Links;

public static class UrlPolicy
{
    public const int MaxLength = 2000;

    public static bool TryNormalize(string? input, [NotNullWhen(true)] out string? normalized, [NotNullWhen(false)] out string? error)
    {
        normalized = null;
        error = null;

        if (input is null || input.Length > MaxLength)
        {
            error = $"Must be at most {MaxLength} characters in length.";
            return false;
        }

        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            error = "Must be an absolute URL.";
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            error = "Must use the http or https scheme.";
            return false;
        }

        if (uri.UserInfo.Length > 0)
        {
            error = "Must not bear embedded credentials.";
            return false;
        }

        if (uri.AbsoluteUri.Length > MaxLength)
        {
            error = "Must not exceed the maximum length.";
            return false;
        }

        normalized = uri.AbsoluteUri;
        return true;
    }
}
