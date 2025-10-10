using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Backend.Hubs;
using Backend.Models;

namespace Backend.Services;

public class TickerHostedService : BackgroundService
{
    private readonly IHubContext<ScoreHub> _hub;

    private const string CourtId = "C1";
    private const string MatchId = "M1";

    // Dva već završena meča (seed), sa vremenima završetka u prošlosti
    private readonly List<FinishedMatchV1> _finished = new()
    {
        new FinishedMatchV1(
            "H. Medjedovic vs J. Sinner",
            new[] { "6:2", "6:4" },
            DateTimeOffset.UtcNow.AddMinutes(-45)
        ),
        new FinishedMatchV1(
            "D. Medvedev vs A. Zverev",
            new[] { "5:7", "2:6" },
            DateTimeOffset.UtcNow.AddMinutes(-25)
        )
    };

    // Jedan budući meč koji će se odigrati (uključujući COURT 2)
    private readonly List<UpcomingMatchV1> _upcoming = new()
    {
        new UpcomingMatchV1("CENTER COURT", "Roger Federer vs Rafael Nadal"),
        new UpcomingMatchV1("COURT 2", "Andy Murray vs Stan Wawrinka")
    };

    public TickerHostedService(IHubContext<ScoreHub> hub)
    {
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Simulacija zadnja 2 poena (prikaz tri uzastopna score stringa,
        // gdje treći implicira kraj meča i reset gem-scora)
        var points = new[]
        {
            "6-4 5-4 30-15",
            "6-4 5-4 40-15",
            "6-4 6-4 0-0"
        };

        foreach (var score in points)
        {
            var payload = new ScorePayloadV1(
                MatchId: MatchId,
                CourtId: CourtId,
                PlayerA: "N. Djokovic",
                PlayerB: "C. Alcaraz",
                Score: score,
                ServerTimeUtc: DateTimeOffset.UtcNow
            );

            await _hub.Clients.All.SendAsync("ScoreUpdate", payload, ct);

            // Pauza 2s između “poena”, osim nakon posljednjeg
            if (score != points[^1])
            {
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }

        // Dodaj treći završeni meč (Djokovic vs Alcaraz)
        _finished.Add(
            new FinishedMatchV1(
                "N. Djokovic vs C. Alcaraz",
                new[] { "6:4", "6:4" },
                DateTimeOffset.UtcNow
            )
        );

        // Prikaži overlay REKLAME (ADS) na 10 sekundi
        await _hub.Clients.All.SendAsync(
            "SceneSwitch",
            new SceneSwitchPayloadV1(CourtId, "ADS", DateTimeOffset.UtcNow),
            ct
        );

        await Task.Delay(TimeSpan.FromSeconds(10), ct);

        // Pošalji SUMARNI prikaz (GOTOVI MEČEVI + najave)
        var summary = new SummaryPayloadV1(
            CourtId,
            _finished
                .OrderBy(f => f.CompletedAt)
                .ToArray(),
            _upcoming.ToArray(),
            DateTimeOffset.UtcNow
        );

        await _hub.Clients.All.SendAsync("SummaryUpdate", summary, ct);

        // Prebaci scenu na SUMMARY (ekran “GOTOVI MEČEVI:”)
        await _hub.Clients.All.SendAsync(
            "SceneSwitch",
            new SceneSwitchPayloadV1(CourtId, "SUMMARY", DateTimeOffset.UtcNow),
            ct
        );

        // Idle petlja (servis ostaje živ)
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
}
