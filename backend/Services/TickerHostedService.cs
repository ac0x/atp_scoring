using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Backend.Hubs;
using Backend.Models;

namespace Backend.Services;

public class TickerHostedService : BackgroundService
{
    private readonly IHubContext<ScoreHub> _hub;
    private const string CourtId = "C1";
    private const string MatchId = "M1";
    private const string NextMatchId = "M2";

    private readonly List<FinishedMatchV1> _finished = new()
    {
        new FinishedMatchV1("H. Medjedovic vs J. Sinner", new[] { "6:2", "6:4" },
            DateTimeOffset.UtcNow.AddMinutes(-45)),
        new FinishedMatchV1("D. Medvedev vs A. Zverev", new[] { "5:7", "2:6" },
            DateTimeOffset.UtcNow.AddMinutes(-25))
    };

    private readonly List<UpcomingMatchV1> _upcoming = new()
    {
        new UpcomingMatchV1("CENTER COURT", "Roger Federer vs Rafael Nadal"),
        new UpcomingMatchV1("COURT 2", "Andy Murray vs Stan Wawrinka")
    };

    private static readonly PlayerCardV1 Federer = new(
        Name: "Roger Federer",
        Country: "Switzerland",
        Rank: 2,
        Age: 41,
        Titles: 103
    );

    private static readonly PlayerCardV1 Nadal = new(
        Name: "Rafael Nadal",
        Country: "Spain",
        Rank: 3,
        Age: 38,
        Titles: 92
    );

    private static readonly H2HRecordV1 FedererVsNadal = new(
        PlayerA: "Roger Federer",
        PlayerB: "Rafael Nadal",
        WinsA: 16,
        WinsB: 24,
        LastMeeting: "Wimbledon 2019"
    );

    public TickerHostedService(IHubContext<ScoreHub> hub) { _hub = hub; }

    private Task BroadcastSceneAsync(string scene, CancellationToken ct) =>
        _hub.Clients.All.SendAsync(
            "SceneSwitch",
            new SceneSwitchPayloadV1(CourtId, scene, DateTimeOffset.UtcNow),
            ct);

    private Task BroadcastAnnounceAsync(
        string step,
        PlayerCardV1? player,
        H2HRecordV1? h2h,
        CancellationToken ct) =>
        _hub.Clients.All.SendAsync(
            "AnnounceNext",
            new AnnounceNextPayloadV1(CourtId, step, player, h2h, DateTimeOffset.UtcNow),
            ct);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
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
            {
                await Task.Delay(TimeSpan.FromSeconds(delay), ct);
            }
        }

        _finished.Add(new FinishedMatchV1("N. Djokovic vs C. Alcaraz", new[] { "6:4", "6:4" }, DateTimeOffset.UtcNow));

        await BroadcastSceneAsync("ADS", ct);
        await Task.Delay(TimeSpan.FromSeconds(5), ct);

        var summary = new SummaryPayloadV1(
            CourtId,
            _finished
                .OrderBy(f => f.CompletedAt)
                .ToArray(),
            _upcoming.ToArray(),
            DateTimeOffset.UtcNow
        );

        await _hub.Clients.All.SendAsync("SummaryUpdate", summary, ct);

        await BroadcastSceneAsync("FINISHED", ct);
        await Task.Delay(TimeSpan.FromSeconds(12), ct);

        await BroadcastSceneAsync("UPCOMING", ct);
        await Task.Delay(TimeSpan.FromSeconds(12), ct);

        await BroadcastSceneAsync("ANNOUNCE_FED", ct);
        await BroadcastAnnounceAsync("ANNOUNCE_FED", Federer, null, ct);
        await Task.Delay(TimeSpan.FromSeconds(5), ct);

        await BroadcastSceneAsync("ANNOUNCE_NAD", ct);
        await BroadcastAnnounceAsync("ANNOUNCE_NAD", Nadal, null, ct);
        await Task.Delay(TimeSpan.FromSeconds(5), ct);

        await BroadcastSceneAsync("ANNOUNCE_H2H", ct);
        await BroadcastAnnounceAsync("ANNOUNCE_H2H", null, FedererVsNadal, ct);
        await Task.Delay(TimeSpan.FromSeconds(6), ct);

        await BroadcastSceneAsync("ANNOUNCE_SIM", ct);

        var announceSimFrames = new (string Score, int DelaySeconds)[]
        {
            ("0-0 0-0", 2),
            ("0-0 15-0", 2),
            ("0-0 30-0", 2),
            ("0-0 40-0", 2),
            ("1-0 0-0", 2)
        };

        foreach (var (score, delay) in announceSimFrames)
        {
            var nextPayload = new ScorePayloadV1(
                MatchId: NextMatchId,
                CourtId: CourtId,
                PlayerA: "R. Federer",
                PlayerB: "R. Nadal",
                Score: score,
                ServerTimeUtc: DateTimeOffset.UtcNow
            );

            await _hub.Clients.All.SendAsync("ScoreUpdate", nextPayload, ct);

            if (delay > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(delay), ct);
            }
        }

        await BroadcastSceneAsync("LIVE", ct);

        var livePayload = new ScorePayloadV1(
            MatchId: NextMatchId,
            CourtId: CourtId,
            PlayerA: "R. Federer",
            PlayerB: "R. Nadal",
            Score: "1-0 0-0",
            ServerTimeUtc: DateTimeOffset.UtcNow
        );

        await _hub.Clients.All.SendAsync("ScoreUpdate", livePayload, ct);

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
        }
    }
}
