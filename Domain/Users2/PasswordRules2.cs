using System.Security.Cryptography;
using System.Text;

namespace Domain.Users2
{
    /// <summary>Password hashing (PBKDF2-SHA256 with a random salt) and the password policy. Pure rules, no database.</summary>
    public static class PasswordRules2
    {
        public const int MinimumLength = 8;
        private const int Iterations = 210_000;
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const string Version = "v1";

        /// <summary>Returns null when the password is acceptable, otherwise the problem.</summary>
        public static string? Validate(string? password)
        {
            if (string.IsNullOrEmpty(password)) return "Password is required.";
            if (password.Length < MinimumLength) return $"Password must be at least {MinimumLength} characters.";
            if (password.Length > 100) return "Password can be at most 100 characters.";
            return null;
        }

        /// <summary>Format: v1$iterations$salt$hash (salt and hash are Base64).</summary>
        public static string Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            return $"{Version}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
        }

        public static bool Verify(string password, string? stored)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(stored)) return false;

            var parts = stored.Split('$');
            if (parts.Length != 4 || parts[0] != Version) return false;
            if (!int.TryParse(parts[1], out int iterations) || iterations < 1) return false;

            try
            {
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expected = Convert.FromBase64String(parts[3]);
                byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>Compares a typed password with an old plain-text password (only used for the one-time upgrade).</summary>
        public static bool LegacyPlainMatches(string typed, string? legacyPlain)
        {
            if (string.IsNullOrEmpty(typed) || string.IsNullOrEmpty(legacyPlain)) return false;
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(typed), Encoding.UTF8.GetBytes(legacyPlain));
        }
    }
}
