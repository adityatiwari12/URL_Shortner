using UrlShortener.Data;
using UrlShortener.Models;
using UrlShortener.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Layered services: Data -> Services -> Controllers.
builder.Services.AddScoped<DatabaseHelper>();
builder.Services.AddScoped<ShortCodeGenerator>();
builder.Services.AddScoped<UrlShortenerService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// GET /{shortCode} - redirects to the original URL and increments the click count.
// Placed after MapControllers/static files so it only matches when nothing else did.
app.MapGet("/{shortCode:length(6)}", (string shortCode, UrlShortenerService service) =>
{
    var originalUrl = service.ResolveAndTrackClick(shortCode);

    if (originalUrl is null)
    {
        return Results.NotFound(new ErrorResponse { Message = "This short link does not exist." });
    }

    return Results.Redirect(originalUrl);
});

app.Run();
