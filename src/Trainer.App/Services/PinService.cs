using System.Security.Cryptography;
using System.Text;

namespace Trainer.App.Services;

/// <summary>
/// Хранит хеш PIN-кода в Preferences. Salt уникален на устройство.
/// Защита от подбора слабая (4-6 цифр), но на локальной БД важнее простота UX.
/// </summary>
public class PinService
{
    private const string HashKey = "pin_hash";
    private const string SaltKey = "pin_salt";

    public bool IsConfigured =>
        !string.IsNullOrEmpty(Preferences.Default.Get<string?>(HashKey, null));

    public void SetPin(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Hash(pin, salt);
        Preferences.Default.Set(SaltKey, Convert.ToBase64String(salt));
        Preferences.Default.Set(HashKey, Convert.ToBase64String(hash));
    }

    public bool Verify(string pin)
    {
        var saltRaw = Preferences.Default.Get<string?>(SaltKey, null);
        var hashRaw = Preferences.Default.Get<string?>(HashKey, null);
        if (saltRaw is null || hashRaw is null) return false;

        var salt = Convert.FromBase64String(saltRaw);
        var expected = Convert.FromBase64String(hashRaw);
        var actual = Hash(pin, salt);

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    public void Reset()
    {
        Preferences.Default.Remove(HashKey);
        Preferences.Default.Remove(SaltKey);
    }

    private static byte[] Hash(string pin, byte[] salt)
    {
        // PBKDF2 100k итераций — нормально для 4-6 цифр PIN-а на локальном устройстве.
        using var kdf = new Rfc2898DeriveBytes(pin, salt, 100_000, HashAlgorithmName.SHA256);
        return kdf.GetBytes(32);
    }
}
