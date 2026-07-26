using System.Text;

namespace TinyLink.Api.Services;

public sealed class ShortenService
{
    private const string Base62Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public long CodeToEncodedId(string shortCode)
    {
        long id = 0;
        foreach (var c in shortCode)
        {
            int index = Base62Alphabet.IndexOf(c);
            if (index == -1) throw new ArgumentException($"Character {c} is not supported");

            id = id * 62 + index;

        }
        return id;
    }

    public string EncodedIdToCode(long id)
    {
        if (id < 0) throw new ArgumentOutOfRangeException(nameof(id), "ID cannot be negative.");
        if (id == 0) return Base62Alphabet[0].ToString();

        var shortCode = new StringBuilder();

        while (id > 0)
        {
            var index = id % 62;
            shortCode.Append(Base62Alphabet[(int)index]);
            id /= 62;
        }

        var charArray = shortCode.ToString().ToCharArray();
        Array.Reverse(charArray);

        return new string(charArray);
    }

}
