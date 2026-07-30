namespace TinyLink.Api.ShortCodes;

public static class Base62
{
    private const string Base62Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public const int CodeLength = 7;
    public const long Domain = 3_521_614_606_208;

    private static readonly sbyte[] Digits = BuildDigitMap();


    public static string Encode(long value)
    {
        if (value < 0 || value >= Domain)
            throw new ArgumentOutOfRangeException(nameof(value), "Value is outside the code domain.");

        return string.Create(CodeLength, value, static (span, v) =>
                {
                    for (var i = CodeLength - 1; i >= 0; i--)
                    {
                        span[i] = Base62Alphabet[(int)(v % 62)];
                        v /= 62;
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
            var digit = Digits[c];
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
        for (var i = 0; i < Base62Alphabet.Length; i++)
        {
            map[Base62Alphabet[i]] = (sbyte)i;
        }
        return map;
    }


}
