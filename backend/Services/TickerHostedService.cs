using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Hubs;
using Backend.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backend.Services;

public class TickerHostedService : BackgroundService
{
    private readonly IHubContext<ScoreHub> _hub;
    private readonly ILogger<TickerHostedService> _logger;
    private readonly ReadinessTracker _readiness;
    private readonly TickerOptions _options;
    private readonly TimeSpan _tickDelay;
    private int _scoreBroadcastCount;

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

    public TickerHostedService(
        IHubContext<ScoreHub> hub,
        IOptionsMonitor<TickerOptions> options,
        ILogger<TickerHostedService> logger,
        ReadinessTracker readiness)
    {
        _hub = hub;
        _logger = logger;
        _readiness = readiness;
        _options = options.CurrentValue ?? new TickerOptions();
        var tickMs = _options.TickMs <= 0 ? 2000 : _options.TickMs;
        _tickDelay = TimeSpan.FromMilliseconds(tickMs);
    }

    private Task BroadcastSceneAsync(string scene, CancellationToken ct)
    {
        _logger.LogInformation("SceneSwitch => {Scene}", scene);
        return _hub.Clients.All.SendAsync(
            "SceneSwitch",
            new SceneSwitchPayloadV1(CourtId, scene, DateTimeOffset.UtcNow),
            ct);
    }

    private Task BroadcastAnnounceAsync(
        string step,
        PlayerCardV1? player,
        H2HRecordV1? h2h,
        CancellationToken ct)
    {
        _logger.LogInformation("AnnounceNext => {Step}", step);
        return _hub.Clients.All.SendAsync(
            "AnnounceNext",
            new AnnounceNextPayloadV1(CourtId, step, player, h2h, DateTimeOffset.UtcNow),
            ct);
    }

    private async Task DelayAsync(int milliseconds, CancellationToken ct)
    {
        if (milliseconds <= 0) return;
        await Task.Delay(TimeSpan.FromMilliseconds(milliseconds), ct);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _readiness.MarkTickerStarted();

        var frames = new (string Score, TimeSpan Delay)[]
        {
            ("6-4 5-4 15-0", _tickDelay),
            ("6-4 5-4 30-0", _tickDelay),
            ("6-4 5-4 30-15", _tickDelay),
            ("6-4 5-4 40-15", _tickDelay),
            ("6-4 6-4 FINAL", TimeSpan.FromMilliseconds(_options.Announce.AdsMs))
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
            _scoreBroadcastCount++;
            _logger.LogInformation("ScoreUpdate count={Count}", _scoreBroadcastCount);

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, ct);
            }
        }

        _finished.Add(new FinishedMatchV1("N. Djokovic vs C. Alcaraz", new[] { "6:4", "6:4" }, DateTimeOffset.UtcNow));

        await BroadcastSceneAsync("ADS", ct);
        await DelayAsync(_options.Announce.AdsMs, ct);

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
        await DelayAsync(_options.Announce.FedererMs, ct);

        await BroadcastSceneAsync("ANNOUNCE_NAD", ct);
        await BroadcastAnnounceAsync("ANNOUNCE_NAD", Nadal, null, ct);
        await DelayAsync(_options.Announce.NadalMs, ct);

        await BroadcastSceneAsync("ANNOUNCE_H2H", ct);
        await BroadcastAnnounceAsync("ANNOUNCE_H2H", null, FedererVsNadal, ct);
        await DelayAsync(_options.Announce.H2HMs, ct);

        await BroadcastSceneAsync("ANNOUNCE_SIM", ct);

        var announceSimFrames = new (string Score, TimeSpan Delay)[]
        {
            ("0-0 0-0", _tickDelay),
            ("0-0 15-0", _tickDelay),
            ("0-0 30-0", _tickDelay),
            ("0-0 40-0", _tickDelay),
            ("1-0 0-0", _tickDelay)
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
            _scoreBroadcastCount++;
            _logger.LogInformation("ScoreUpdate count={Count}", _scoreBroadcastCount);

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, ct);
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
        _scoreBroadcastCount++;
        _logger.LogInformation("ScoreUpdate count={Count}", _scoreBroadcastCount);

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
        }
    }
}
