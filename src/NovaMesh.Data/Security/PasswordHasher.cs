using System.Security.Cryptography;
using System.Text;

namespace NovaMesh.Data.Security;

public static class PasswordHasher
{
    public static void HashPassword(string password, out string hash, out string salt)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        salt = Convert.ToBase64String(saltBytes);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            iterations: 100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32);
        hash = Convert.ToBase64String(hashBytes);
    }

    public static bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        var saltBytes = Convert.FromBase64String(storedSalt);
        var computedBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            iterations: 100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32);
        return CryptographicOperations.FixedTimeEquals(computedBytes, Convert.FromBase64String(storedHash));
    }
}