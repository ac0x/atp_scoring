using System.Linq;
using Backend.Hubs;
using Backend.Models;
using Backend.Services;
using Backend.SmartDirectorListener.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "LocalDev";

// CORS: čita dozvoljene origin-e iz konfiguracije; fallback na localhost:5173
var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?.Where(o => !string.IsNullOrWhiteSpace(o))
    .Select(o => o.Trim())
    .Where(o => o.Length > 0)
    .ToArray();

if (configuredOrigins is not { Length: > 0 })
{
    configuredOrigins = new[] { "http://localhost:5173", "https://localhost:5173" };
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy
            .WithOrigins(configuredOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// SignalR (zadržava PascalCase kontrakt)
builder.Services.AddSignalR().AddJsonProtocol(o =>
{
    o.PayloadSerializerOptions.PropertyNamingPolicy = null;
});

// Background ticker i readiness tracking
builder.Services.AddSingleton<ReadinessTracker>();
builder.Services.Configure<TickerOptions>(builder.Configuration.GetSection("Ticker"));
builder.Services.AddSingleton<TickerHostedService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<TickerHostedService>());
builder.Services.AddSmartDirectorListener(builder.Configuration);

var app = builder.Build();

app.UseCors(CorsPolicy);

// Mock REST rute
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

// SignalR hub
app.MapHub<ScoreHub>("/hubs/score");
app.MapHub<LiveHub>("/live");
app.Services.GetRequiredService<ReadinessTracker>().MarkHubReady();

app.Run();
