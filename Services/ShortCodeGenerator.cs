using System.Security.Cryptography;
using UrlShortener.Data;

namespace UrlShortener.Services
{
    /// <summary>
    /// Generates random alphanumeric shortcodes and guarantees uniqueness against the database.
    /// </summary>
    public class ShortCodeGenerator
    {
        private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private const int CodeLength = 6;
        private const int MaxAttempts = 10;

        private readonly DatabaseHelper _databaseHelper;

        public ShortCodeGenerator(DatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper;
        }

        /// <summary>
        /// Produces a 6-character shortcode that does not already exist in the Urls table.
        /// </summary>
        public string GenerateUniqueShortCode()
        {
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var candidate = GenerateRandomCode();
                if (!_databaseHelper.ShortCodeExists(candidate))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("Could not generate a unique short code. Please try again.");
        }

        private static string GenerateRandomCode()
        {
            Span<char> buffer = stackalloc char[CodeLength];
            for (var i = 0; i < CodeLength; i++)
            {
                var index = RandomNumberGenerator.GetInt32(Characters.Length);
                buffer[i] = Characters[index];
            }
            return new string(buffer);
        }
    }
}
