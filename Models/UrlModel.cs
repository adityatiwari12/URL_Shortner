namespace UrlShortener.Models
{
    /// <summary>
    /// Represents a single row of the Urls table.
    /// </summary>
    public class UrlModel
    {
        public int Id { get; set; }
        public string OriginalUrl { get; set; } = string.Empty;
        public string ShortCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ClickCount { get; set; }
    }

    /// <summary>
    /// Body sent by the frontend when creating a short URL.
    /// </summary>
    public class CreateUrlRequest
    {
        public string OriginalUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response returned after a short URL is created.
    /// </summary>
    public class CreateUrlResponse
    {
        public int Id { get; set; }
        public string OriginalUrl { get; set; } = string.Empty;
        public string ShortCode { get; set; } = string.Empty;
        public string ShortUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ClickCount { get; set; }
    }

    /// <summary>
    /// Generic error payload returned to the frontend.
    /// </summary>
    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
