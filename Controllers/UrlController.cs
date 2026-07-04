using Microsoft.AspNetCore.Mvc;
using UrlShortener.Models;
using UrlShortener.Services;

namespace UrlShortener.Controllers
{
    [ApiController]
    [Route("api/url")]
    public class UrlController : ControllerBase
    {
        private readonly UrlShortenerService _urlShortenerService;
        private readonly ILogger<UrlController> _logger;

        public UrlController(UrlShortenerService urlShortenerService, ILogger<UrlController> logger)
        {
            _urlShortenerService = urlShortenerService;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/url - creates a shortened URL.
        /// </summary>
        [HttpPost]
        public IActionResult CreateShortUrl([FromBody] CreateUrlRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.OriginalUrl))
            {
                return BadRequest(new ErrorResponse { Message = "Please enter a valid URL." });
            }

            if (!UrlShortenerService.IsValidUrl(request.OriginalUrl))
            {
                return BadRequest(new ErrorResponse { Message = "Please enter a valid URL." });
            }

            try
            {
                var response = _urlShortenerService.CreateShortUrl(request.OriginalUrl);
                return CreatedAtAction(nameof(CreateShortUrl), new { shortCode = response.ShortCode }, response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create short URL for {OriginalUrl}", request.OriginalUrl);
                return StatusCode(500, new ErrorResponse { Message = "Something went wrong while creating the short URL. Please try again." });
            }
        }

        /// <summary>
        /// GET /api/url - returns every shortened URL, newest first.
        /// </summary>
        [HttpGet]
        public IActionResult GetAllUrls()
        {
            try
            {
                var urls = _urlShortenerService.GetAllUrls();
                return Ok(urls);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load URLs");
                return StatusCode(500, new ErrorResponse { Message = "Could not load links right now. Please try again." });
            }
        }

        /// <summary>
        /// DELETE /api/url/{shortCode} - removes one URL.
        /// </summary>
        [HttpDelete("{shortCode}")]
        public IActionResult DeleteUrl(string shortCode)
        {
            try
            {
                var deleted = _urlShortenerService.DeleteUrl(shortCode);
                if (!deleted)
                {
                    return NotFound(new ErrorResponse { Message = "Link not found." });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete short URL {ShortCode}", shortCode);
                return StatusCode(500, new ErrorResponse { Message = "Could not delete the link. Please try again." });
            }
        }
    }
}
