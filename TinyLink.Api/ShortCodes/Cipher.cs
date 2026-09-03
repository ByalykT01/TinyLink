using System.Buffers.Binary;
using System.Security.Cryptography;

namespace TinyLink.Api.ShortCodes;

public sealed class Cipher
{
    private const int _rounds = 10;
    private const int _halfBits = 21;

    // The 42-bit code space splits into two 21-bit halves for the Feistel rounds.
    private const uint _halfMask = (1u << _halfBits) - 1;
    private const int _minKeyBytes = 32;

    private readonly byte[] _key;

    public Cipher(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentOutOfRangeException.ThrowIfLessThan(key.Length, _minKeyBytes);
        _key = [.. key];
    }

    internal long Permute(long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, Base62.Domain);

        var current = value;

        do
        {
            current = Encrypt(current);
        }
        while (current >= Base62.Domain);

        return current;
    }

    private long Encrypt(long value)
    {
        // extracting values of two 21-bit positions
        var left = (uint)((value >> _halfBits) & _halfMask);
        var right = (uint)(value & _halfMask);

        for (var round = 0; round < _rounds; round++)
        {
            (left, right) = (right, left ^ RoundFunction(round, right));
        }
        return ((long)left << _halfBits) | right;
    }

    private uint RoundFunction(int round, uint value)
    {
        Span<byte> input = stackalloc byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(input, round);
        BinaryPrimitives.WriteUInt32LittleEndian(input[4..], value);

        Span<byte> hash = stackalloc byte[HMACSHA256.HashSizeInBytes];
        HMACSHA256.HashData(_key, input, hash);

        // trim the half's bit so that XOR won't spill into the other half
        return BinaryPrimitives.ReadUInt32LittleEndian(hash) & _halfMask;
    }

}
