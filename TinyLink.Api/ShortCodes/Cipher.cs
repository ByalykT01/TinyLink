using System.Buffers.Binary;
using System.Security.Cryptography;

namespace TinyLink.Api.ShortCodes;

public sealed class Cipher
{
    private const int Rounds = 10;
    private const int HalfBits = 21;

    // firstly, 23-bit integer's bit is getting moved 21 bits to the left
    // secondly, 1 is substracted, leaving with 32 bit with 20 last bits being one
    private const uint HalfMask = (1u << HalfBits) - 1;
    private const int MinKeyBytes = 32;

    private readonly byte[] _key;

    public Cipher(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentOutOfRangeException.ThrowIfLessThan(key.Length, MinKeyBytes);
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
        var left = (uint)((value >> HalfBits) & HalfMask);
        var right = (uint)(value & HalfMask);

        for (var round = 0; round < Rounds; round++)
        {
            (left, right) = (right, left ^ RoundFunction(round, right));
        }
        return ((long)left << HalfBits) | right;
    }

    private uint RoundFunction(int round, uint value)
    {
        Span<byte> input = stackalloc byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(input, round);
        BinaryPrimitives.WriteUInt32LittleEndian(input[4..], value);

        Span<byte> hash = stackalloc byte[HMACSHA256.HashSizeInBytes];
        HMACSHA256.HashData(_key, input, hash);

        // trim the half's bit so that XOR won't spill into the other half
        return BinaryPrimitives.ReadUInt32LittleEndian(hash) & HalfMask;
    }

}
