using UrlShortener.Data;
using UrlShortener.Models;

namespace UrlShortener.Services
{
    /// <summary>
    /// Business logic for the URL shortener: validation, shortcode generation,
    /// and orchestration of database calls. Controllers should not talk to
    /// DatabaseHelper directly.
    /// </summary>
    public class UrlShortenerService
    {
        private readonly DatabaseHelper _databaseHelper;
        private readonly ShortCodeGenerator _shortCodeGenerator;

        public UrlShortenerService(DatabaseHelper databaseHelper, ShortCodeGenerator shortCodeGenerator)
        {
            _databaseHelper = databaseHelper;
            _shortCodeGenerator = shortCodeGenerator;
        }

        /// <summary>
        /// Validates the given string is a well-formed absolute http/https URL.
        /// </summary>
        public static bool IsValidUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            return Uri.TryCreate(url, UriKind.Absolute, out var result)
                && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Creates a shortened URL for the given original URL.
        /// </summary>
        public CreateUrlResponse CreateShortUrl(string originalUrl, string baseUrl)
        {
            if (!IsValidUrl(originalUrl))
            {
                throw new ArgumentException("Please enter a valid URL.");
            }

            var shortCode = _shortCodeGenerator.GenerateUniqueShortCode();
            var created = _databaseHelper.InsertUrl(originalUrl, shortCode);

            return new CreateUrlResponse
            {
                Id = created.Id,
                OriginalUrl = created.OriginalUrl,
                ShortCode = created.ShortCode,
                ShortUrl = BuildShortUrl(baseUrl, created.ShortCode),
                CreatedAt = created.CreatedAt,
                ClickCount = created.ClickCount
            };
        }

        /// <summary>
        /// Returns all shortened URLs, newest first, shaped for the frontend.
        /// </summary>
        public List<CreateUrlResponse> GetAllUrls(string baseUrl)
        {
            return _databaseHelper.GetAllUrls()
                .Select(u => new CreateUrlResponse
                {
                    Id = u.Id,
                    OriginalUrl = u.OriginalUrl,
                    ShortCode = u.ShortCode,
                    ShortUrl = BuildShortUrl(baseUrl, u.ShortCode),
                    CreatedAt = u.CreatedAt,
                    ClickCount = u.ClickCount
                })
                .ToList();
        }

        /// <summary>
        /// Resolves a shortcode to its original URL and increments the click counter.
        /// Returns null if the shortcode does not exist.
        /// </summary>
        public string? ResolveAndTrackClick(string shortCode)
        {
            var record = _databaseHelper.GetByShortCode(shortCode);
            if (record is null)
            {
                return null;
            }

            _databaseHelper.IncrementClickCount(shortCode);
            return record.OriginalUrl;
        }

        /// <summary>
        /// Deletes a shortened URL. Returns true if a record was removed.
        /// </summary>
        public bool DeleteUrl(string shortCode)
        {
            return _databaseHelper.DeleteByShortCode(shortCode);
        }

        private static string BuildShortUrl(string baseUrl, string shortCode)
        {
            return $"{baseUrl.TrimEnd('/')}/{shortCode}";
        }
    }
}
