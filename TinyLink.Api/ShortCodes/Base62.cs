namespace TinyLink.Api.ShortCodes;

public static class Base62
{
    private const string _base62Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public const int CodeLength = 7;
    public const long Domain = 3_521_614_606_208;

    private static readonly sbyte[] _digits = BuildDigitMap();


    public static string Encode(long value)
    {

        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, Domain);

        return string.Create(CodeLength, value, static (span, v) =>
                {
                    for (var i = CodeLength - 1; i >= 0; i--)
                    {
                        (v, var digit) = Math.DivRem(v, 62);
                        span[i] = _base62Alphabet[(int)digit];
                    }
                });
    }

    public static bool TryDecode(ReadOnlySpan<char> code, out long value)
    {
        value = 0;
        if (code.Length != 7) return false;

        long result = 0;
        foreach (var c in code)
        {
            if (c > 127) return false;
            var digit = _digits[c];
            if (digit < 0) return false;
            result = result * 62 + digit;
        }
        value = result;

        return true;
    }

    private static sbyte[] BuildDigitMap()
    {
        var map = new sbyte[128];
        Array.Fill(map, (sbyte)-1);
        for (var i = 0; i < _base62Alphabet.Length; i++)
        {
            map[_base62Alphabet[i]] = (sbyte)i;
        }
        return map;
    }


}
