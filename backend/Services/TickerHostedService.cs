using Microsoft.AspNetCore.SignalR;
using Backend.Hubs;
using Backend.Models;

namespace Backend.Services;

public class TickerHostedService : BackgroundService
{
    private readonly IHubContext<ScoreHub> _hub;
    private readonly string courtId = "C1";
    private string currentMatchId = "M1";
    private bool switched = false; // da ADS/LIVE pošaljemo samo jednom

    private readonly string[] seq = new[] {
        "6-4 2-3 30-15", "6-4 3-3 30-30", "6-4 3-4 15-30",
        "6-4 4-4 15-0",  "6-4 5-4 40-15", "6-4 6-4 0-0"
    };

    public TickerHostedService(IHubContext<ScoreHub> hub) { _hub = hub; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var i = 0;

        while (!ct.IsCancellationRequested)
        {
            // --- Pošalji SCORE svake ~2 s ---
            var payload = new ScorePayloadV1(
                MatchId: currentMatchId,
                CourtId: courtId,
                PlayerA: currentMatchId == "M1" ? "N. Djokovic" : "R. Nadal",
                PlayerB: currentMatchId == "M1" ? "C. Alcaraz"  : "R. Federer",
                Score: seq[i % seq.Length],
                ServerTimeUtc: DateTimeOffset.UtcNow
            );
            await _hub.Clients.All.SendAsync("ScoreUpdate", payload, ct);
            i++;

            // --- Jednom uradi ADS → LIVE i promijeni meč ---
            if (!switched && i == 8) // kad izbrojimo do 8 tickova
            {
                // prikaži "REKLAME" 5 sekundi
                await _hub.Clients.All.SendAsync("SceneSwitch",
                    new SceneSwitchPayloadV1(courtId, "ADS", DateTimeOffset.UtcNow), ct);

                await Task.Delay(TimeSpan.FromSeconds(5), ct);

                // pređi na novi meč i vrati u LIVE
                currentMatchId = "M3";

                await _hub.Clients.All.SendAsync("SceneSwitch",
                    new SceneSwitchPayloadV1(courtId, "LIVE", DateTimeOffset.UtcNow), ct);

                switched = true;
            }

            // --- pauza između tickova ---
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }
}
