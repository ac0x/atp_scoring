using Backend.Hubs;
using Backend.Models;
using Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// CORS za frontend dev server
const string CorsPolicy = "LocalDev";
builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, p => p
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
));

// SignalR (zadržavamo PascalCase u v1)
builder.Services.AddSignalR().AddJsonProtocol(o => {
    o.PayloadSerializerOptions.PropertyNamingPolicy = null;
});

// Background ticker
builder.Services.AddHostedService<TickerHostedService>();

var app = builder.Build();
app.UseCors(CorsPolicy);

// Mock REST snapshot rute
app.MapGet("/api/courts", () => new [] {
    new Court("C1", "Center Court"),
    new Court("C2", "Court 2")
});

var matches = new [] {
    new MatchSummary("M1", "C1", "N. Djokovic", "C. Alcaraz"),
    new MatchSummary("M2", "C1", "R. Federer", "R. Nadal"),
    new MatchSummary("M3", "C2", "A. Murray", "S. Wawrinka")    
};
app.MapGet("/api/matches", () => matches);

// SignalR hub
app.MapHub<ScoreHub>("/hubs/score");

app.Run();
