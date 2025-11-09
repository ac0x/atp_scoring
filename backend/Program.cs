using Backend.Hubs;
using Backend.Models;
using Backend.Services;
using Backend.SmartDirectorListener.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "LocalDev";

// Allowed origins from config (fallback to localhost)
var allowedOrigins = (builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
    .Select(o => o?.Trim())
    .Where(o => !string.IsNullOrWhiteSpace(o))
    .Distinct()
    .ToArray();

if (allowedOrigins.Length == 0)
{
    allowedOrigins = new[] { "http://localhost:5173", "https://localhost:5173" };
}

builder.Services.AddCors(o =>
{
    o.AddPolicy(CorsPolicyName, p =>
        p.WithOrigins(allowedOrigins)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());
});

// SignalR: keep PascalCase contract
builder.Services.AddSignalR().AddJsonProtocol(o =>
{
    o.PayloadSerializerOptions.PropertyNamingPolicy = null;
});

// Background ticker, options & readiness
builder.Services.AddSingleton<ReadinessTracker>();
builder.Services.Configure<TickerOptions>(builder.Configuration.GetSection("Ticker"));
builder.Services.AddHostedService<TickerHostedService>();

// SmartDirector listener
builder.Services.AddSmartDirectorListener(builder.Configuration);

var app = builder.Build();

app.UseCors(CorsPolicyName);

// Mock REST routes
app.MapGet("/api/courts", () => new[]
{
    new Court("C1", "Center Court"),
    new Court("C2", "Court 2")
});

var matches = new[]
{
    new MatchSummary("M1", "C1", "N. Djokovic", "C. Alcaraz"),
    new MatchSummary("M2", "C1", "R. Federer", "R. Nadal"),
    new MatchSummary("M3", "C2", "A. Murray", "S. Wawrinka")
};
app.MapGet("/api/matches", () => matches);

// Health endpoints
app.MapGet("/healthz", () => Results.Json(new
{
    status = "ok",
    timeUtc = DateTimeOffset.UtcNow
}));

app.MapGet("/readyz", (ReadinessTracker readiness) =>
{
    var ready = readiness.IsReady;
    var payload = new
    {
        status = ready ? "ready" : "starting",
        tickerStarted = readiness.TickerStarted,
        hubReady = readiness.HubReady,
        timeUtc = DateTimeOffset.UtcNow
    };

    return ready
        ? Results.Json(payload)
        : Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable);
});

// SignalR hubs
app.MapHub<ScoreHub>("/hubs/score");
app.MapHub<LiveHub>("/live");

// Mark hub ready
app.Services.GetRequiredService<ReadinessTracker>().MarkHubReady();

app.Run();
