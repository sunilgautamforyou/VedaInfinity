using System.Security.Cryptography;
using System.Text;

namespace VedaInfinity.Web.Services;

/// <summary>
/// SHA1 hasher to match MySQL SHA1() as requested by school team.
/// Prefer stronger hashing (bcrypt/Argon2) for production upgrades.
/// </summary>
public static class PasswordHasher
{
    public static string HashSha1(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var hash = SHA1.HashData(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    public static bool VerifySha1(string plainText, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
            return false;
        var computed = HashSha1(plainText);
        return string.Equals(computed, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
