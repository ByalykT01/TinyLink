using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace TinyLink.Api.Features.Links;

internal static class DeleteToken
{
    private const int _tokenSizeInBytes = 32;

    public const int HashSizeInBytes = SHA256.HashSizeInBytes;

    public static (string Value, byte[] Hash) Generate()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(_tokenSizeInBytes);

        return (
            WebEncoders.Base64UrlEncode(tokenBytes),
            SHA256.HashData(tokenBytes));
    }

    public static bool Matches(string? value, byte[]? expectedHash)
    {
        if (string.IsNullOrWhiteSpace(value) || expectedHash is null || expectedHash.Length != HashSizeInBytes)
            return false;

        byte[] tokenBytes;
        try
        {
            tokenBytes = WebEncoders.Base64UrlDecode(value);
        }
        catch (FormatException)
        {
            return false;
        }

        if (tokenBytes.Length != _tokenSizeInBytes)
            return false;

        var actualHash = SHA256.HashData(tokenBytes);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

}
