using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Trainer.Api.Auth;

/// <summary>
/// Argon2id with parameters tuned for ~100ms on a typical Fly.io shared-cpu-1x VM.
/// Hash format: argon2id$v=19$m=&lt;mem&gt;$t=&lt;iters&gt;$p=&lt;parallelism&gt;$&lt;base64(salt)&gt;$&lt;base64(hash)&gt;
/// </summary>
public class PasswordHasher
{
    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private const int MemoryKb = 19456;   // ~19 MiB
    private const int Iterations = 2;
    private const int Parallelism = 1;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Compute(password, salt);
        return $"argon2id$v=19$m={MemoryKb}$t={Iterations}$p={Parallelism}$" +
               $"{Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string encoded)
    {
        var parts = encoded.Split('$');
        if (parts.Length != 7 || parts[0] != "argon2id") return false;

        try
        {
            var mem = int.Parse(parts[2].Substring(2));
            var iter = int.Parse(parts[3].Substring(2));
            var par = int.Parse(parts[4].Substring(2));
            var salt = Convert.FromBase64String(parts[5]);
            var expected = Convert.FromBase64String(parts[6]);

            var actual = Compute(password, salt, mem, iter, par, expected.Length);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] Compute(
        string password,
        byte[] salt,
        int memoryKb = MemoryKb,
        int iterations = Iterations,
        int parallelism = Parallelism,
        int hashBytes = HashBytes)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memoryKb,
            Iterations = iterations,
            DegreeOfParallelism = parallelism,
        };
        return argon2.GetBytes(hashBytes);
    }
}
