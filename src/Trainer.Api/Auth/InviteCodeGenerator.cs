using System.Security.Cryptography;

namespace Trainer.Api.Auth;

public static class InviteCodeGenerator
{
    // Crockford base32-ish alphabet: no 0/O/1/I/L to avoid confusion when read out loud.
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public static string Generate(int length = 8)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }
        return new string(chars);
    }
}
