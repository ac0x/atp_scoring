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

    // Seed: dva završena meča
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

    // Najave (uključujući COURT 2)
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
        // Frame-ovi za završna 2 poena + FINAL
        var frames = new (string Score, int DelaySeconds)[]
        {
            ("6-4 5-4 15-0", 4),
            ("6-4 5-4 30-0", 4),
            ("6-4 5-4 30-15", 4),
            ("6-4 5-4 40-15", 4),
            ("6-4 6-4 FINAL", 5)
        };

        foreach (var (score, delay) in frames)
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

            if (delay > 0)
                await Task.Delay(TimeSpan.FromSeconds(delay), ct);
        }

        // Dodaj treći završeni (Djokovic vs Alcaraz)
        _finished.Add(new FinishedMatchV1(
            "N. Djokovic vs C. Alcaraz",
            new[] { "6:4", "6:4" },
            DateTimeOffset.UtcNow
        ));

        // ADS (5s)
        await _hub.Clients.All.SendAsync(
            "SceneSwitch",
            new SceneSwitchPayloadV1(CourtId, "ADS", DateTimeOffset.UtcNow),
            ct
        );
        await Task.Delay(TimeSpan.FromSeconds(20), ct);

        // Pošalji rezime (gotovi + najave)
        var summary = new SummaryPayloadV1(
            CourtId,
            _finished
                .OrderBy(f => f.CompletedAt)
                .ToArray(),
            _upcoming.ToArray(),
            DateTimeOffset.UtcNow
        );
        await _hub.Clients.All.SendAsync("SummaryUpdate", summary, ct);

        // Prikaži GOTOVI (FINISHED)
        await _hub.Clients.All.SendAsync(
            "SceneSwitch",
            new SceneSwitchPayloadV1(CourtId, "FINISHED", DateTimeOffset.UtcNow),
            ct
        );

        // Nakon 12s prikaži UPCOMING
        await Task.Delay(TimeSpan.FromSeconds(12), ct);
        await _hub.Clients.All.SendAsync(
            "SceneSwitch",
            new SceneSwitchPayloadV1(CourtId, "UPCOMING", DateTimeOffset.UtcNow),
            ct
        );

        // Idle loop
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
}
