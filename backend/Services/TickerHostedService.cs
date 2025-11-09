using System;
using System.Collections.Concurrent;
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
    private readonly TimeSpan _manualHoldPollDelay;

    private int _scoreBroadcastCount;

    private const string Court1Id = "C1";
    private const string Court1MatchId = "M1";
    private const string Court1NextMatchId = "M2";

    private const string Court2Id = "C2";
    private const string Court2MatchId = "M_C2";

    private readonly ConcurrentDictionary<string, string?> _manualHoldReasonByCourt = new();
    private readonly ConcurrentDictionary<string, string> _lastSceneByCourt = new();

    private readonly List<FinishedMatchV1> _finished = new()
    {
        new FinishedMatchV1("H. Medjedovic vs J. Sinner", new[] { "6:2", "6:4" }, DateTimeOffset.UtcNow.AddMinutes(-45)),
        new FinishedMatchV1("D. Medvedev vs A. Zverev", new[] { "5:7", "2:6" }, DateTimeOffset.UtcNow.AddMinutes(-25))
    };

    private readonly List<UpcomingMatchV1> _upcoming = new()
    {
        new UpcomingMatchV1("CENTER COURT", "Roger Federer vs Rafael Nadal"),
        new UpcomingMatchV1("COURT 2", "Andy Murray vs Stan Wawrinka")
    };

    private static readonly PlayerCardV1 Federer = new(
        Name: "Roger Federer", Country: "Switzerland", Rank: 2, Age: 41, Titles: 103);

    private static readonly PlayerCardV1 Nadal = new(
        Name: "Rafael Nadal", Country: "Spain", Rank: 3, Age: 38, Titles: 92);

    private static readonly H2HRecordV1 FedererVsNadal = new(
        PlayerA: "Roger Federer", PlayerB: "Rafael Nadal", WinsA: 16, WinsB: 24, LastMeeting: "Wimbledon 2019");

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

        var tickMs = _options.DefaultTickMs > 0
            ? _options.DefaultTickMs
            : _options.TickMs > 0
                ? _options.TickMs
                : 2000;

        _tickDelay = TimeSpan.FromMilliseconds(tickMs);

        var pollMs = _options.ManualAdsMaxMs > 0
            ? Math.Min(_options.ManualAdsMaxMs, tickMs)
            : tickMs;

        pollMs = Math.Max(pollMs, 500);
        _manualHoldPollDelay = TimeSpan.FromMilliseconds(pollMs);
    }

    // Public API za ručni hold sa režijom
    public Task SetManualHoldAsync(string courtId, string reason)
    {
        if (string.IsNullOrWhiteSpace(courtId)) return Task.CompletedTask;

        var safeReason = string.IsNullOrWhiteSpace(reason) ? "MANUAL" : reason;
        _manualHoldReasonByCourt[courtId] = safeReason;
        _logger.LogInformation("Manual hold => {CourtId} ({Reason})", courtId, safeReason);
        return BroadcastSceneAsync(courtId, "ADS", CancellationToken.None);
    }

    public async Task ClearManualHoldAsync(string courtId)
    {
        if (string.IsNullOrWhiteSpace(courtId)) return;

        _manualHoldReasonByCourt[courtId] = null;
        _logger.LogInformation("Manual hold cleared => {CourtId}", courtId);
        await BroadcastSceneAsync(courtId, "LIVE", CancellationToken.None);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _readiness.MarkTickerStarted();

        _manualHoldReasonByCourt.TryAdd(Court1Id, null);
        _manualHoldReasonByCourt.TryAdd(Court2Id, null);

        var c1 = RunCourt1Async(stoppingToken);
        var c2 = RunCourt2Async(stoppingToken);

        await Task.WhenAll(c1, c2);
    }

    // --- Emit helpers --------------------------------------------------------

    private Task BroadcastSceneAsync(string courtId, string scene, CancellationToken ct)
    {
        var current = _lastSceneByCourt.GetOrAdd(courtId, _ => string.Empty);
        if (string.Equals(current, scene, StringComparison.Ordinal))
            return Task.CompletedTask;

        _lastSceneByCourt[courtId] = scene;
        _logger.LogInformation("SceneSwitch[{Court}] => {Scene}", courtId, scene);

        return _hub.Clients.All.SendAsync(
            "SceneSwitch",
            new SceneSwitchPayloadV1(courtId, scene, DateTimeOffset.UtcNow),
            ct);
    }

    private Task BroadcastAnnounceAsync(
        string courtId,
        string step,
        PlayerCardV1? player,
        H2HRecordV1? h2H,
        CancellationToken ct)
    {
        _logger.LogInformation("AnnounceNext[{Court}] => {Step}", courtId, step);

        return _hub.Clients.All.SendAsync(
            "AnnounceNext",
            new AnnounceNextPayloadV1(courtId, step, player, h2H, DateTimeOffset.UtcNow),
            ct);
    }

    private async Task BroadcastScoreAsync(
        string courtId,
        string matchId,
        string playerA,
        string playerB,
        string score,
        CancellationToken ct)
    {
        await WaitForManualHoldReleaseAsync(courtId, ct);
        await BroadcastSceneAsync(courtId, "LIVE", ct);

        var payload = new ScorePayloadV1(
            MatchId: matchId,
            CourtId: courtId,
            PlayerA: playerA,
            PlayerB: playerB,
            Score: score,
            ServerTimeUtc: DateTimeOffset.UtcNow);

        await _hub.Clients.All.SendAsync("ScoreUpdate", payload, ct);
        _scoreBroadcastCount++;
        _logger.LogInformation("ScoreUpdate[{Court}] #{Count} => {Score}", courtId, _scoreBroadcastCount, score);
    }

    private async Task WaitForManualHoldReleaseAsync(string courtId, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (!_manualHoldReasonByCourt.TryGetValue(courtId, out var reason) || string.IsNullOrEmpty(reason))
                return;

            _logger.LogDebug("Court {CourtId} waiting on manual hold ({Reason})", courtId, reason);
            await BroadcastSceneAsync(courtId, "ADS", ct);
            await Task.Delay(_manualHoldPollDelay, ct);
        }
    }

    private async Task RunAdsSequenceAsync(
        string courtId,
        int? preDelayMs,
        int durationMs,
        CancellationToken ct)
    {
        if (preDelayMs is > 0) await DelayAsync(preDelayMs.Value, ct);

        await BroadcastSceneAsync(courtId, "ADS", ct);

        if (durationMs > 0) await DelayAsync(durationMs, ct);

        await WaitForManualHoldReleaseAsync(courtId, ct);
        await BroadcastSceneAsync(courtId, "LIVE", ct);
    }

    private async Task MaybeBreakAfterGameAsync(
        string courtId,
        int gamesInSet,
        bool isSetFinished,
        CancellationToken ct,
        int? preDelayOverrideMs = null,
        int? changeoverOverrideMs = null)
    {
        if (isSetFinished)
        {
            var setBreak = _options.SetBreakAdsMs > 0 ? _options.SetBreakAdsMs : 120_000;
            var preDelay = preDelayOverrideMs ?? _options.PreAdsDelayMs;
            await RunAdsSequenceAsync(courtId, preDelay, setBreak, ct);
            return;
        }

        if (gamesInSet > 1 && gamesInSet % 2 == 1)
        {
            var duration = changeoverOverrideMs ?? _options.ChangeoverAdsMs;
            if (duration > 0)
            {
                var preDelay = preDelayOverrideMs ?? _options.PreAdsDelayMs;
                await RunAdsSequenceAsync(courtId, preDelay, duration, ct);
            }
        }
    }

    private static Task DelayAsync(int milliseconds, CancellationToken ct)
        => milliseconds <= 0 ? Task.CompletedTask : Task.Delay(TimeSpan.FromMilliseconds(milliseconds), ct);

    // --- Court flows ---------------------------------------------------------

    private async Task RunCourt1Async(CancellationToken ct)
    {
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
            await BroadcastScoreAsync(Court1Id, Court1MatchId, "N. Djokovic", "C. Alcaraz", score, ct);
            if (delay > TimeSpan.Zero) await Task.Delay(delay, ct);
        }

        _finished.Add(new FinishedMatchV1("N. Djokovic vs C. Alcaraz", new[] { "6:4", "6:4" }, DateTimeOffset.UtcNow));

        await RunAdsSequenceAsync(Court1Id, _options.PreAdsDelayMs, _options.Announce.AdsMs, ct);

        var summary = new SummaryPayloadV1(
            Court1Id,
            _finished.OrderBy(f => f.CompletedAt).ToArray(),
            _upcoming.ToArray(),
            DateTimeOffset.UtcNow);

        await _hub.Clients.All.SendAsync("SummaryUpdate", summary, ct);

        await BroadcastSceneAsync(Court1Id, "FINISHED", ct);
        await Task.Delay(TimeSpan.FromSeconds(12), ct);

        await BroadcastSceneAsync(Court1Id, "UPCOMING", ct);
        await Task.Delay(TimeSpan.FromSeconds(12), ct);

        await BroadcastSceneAsync(Court1Id, "ANNOUNCE_FED", ct);
        await BroadcastAnnounceAsync(Court1Id, "ANNOUNCE_FED", Federer, null, ct);
        await DelayAsync(_options.Announce.FedererMs, ct);

        await BroadcastSceneAsync(Court1Id, "ANNOUNCE_NAD", ct);
        await BroadcastAnnounceAsync(Court1Id, "ANNOUNCE_NAD", Nadal, null, ct);
        await DelayAsync(_options.Announce.NadalMs, ct);

        await BroadcastSceneAsync(Court1Id, "ANNOUNCE_H2H", ct);
        await BroadcastAnnounceAsync(Court1Id, "ANNOUNCE_H2H", null, FedererVsNadal, ct);
        await DelayAsync(_options.Announce.H2HMs, ct);

        await BroadcastSceneAsync(Court1Id, "ANNOUNCE_SIM", ct);

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
            await BroadcastScoreAsync(Court1Id, Court1NextMatchId, "R. Federer", "R. Nadal", score, ct);
            if (delay > TimeSpan.Zero) await Task.Delay(delay, ct);
        }

        await BroadcastSceneAsync(Court1Id, "LIVE", ct);
        await BroadcastScoreAsync(Court1Id, Court1NextMatchId, "R. Federer", "R. Nadal", "1-0 0-0", ct);

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
        }
    }

    private async Task RunCourt2Async(CancellationToken ct)
    {
        var courtOptions = _options.C2 ?? new TickerOptions.CourtOptions();
        var preAdsDelay = courtOptions.PreAdsDelayMs > 0 ? courtOptions.PreAdsDelayMs : _options.PreAdsDelayMs;
        var changeoverDuration = courtOptions.PostGemAdsMs > 0 ? courtOptions.PostGemAdsMs : _options.ChangeoverAdsMs;

        await BroadcastScoreAsync(Court2Id, Court2MatchId, "L. Djere", "D. Lajovic", "0-0 0-0", ct);
        await Task.Delay(_tickDelay, ct);

        var gamePlan = new[]
        {
            (Score: "1-0 0-0", GamesA: 1, GamesB: 0),
            (Score: "2-0 0-0", GamesA: 2, GamesB: 0),
            (Score: "3-0 0-0", GamesA: 3, GamesB: 0),
            (Score: "4-0 0-0", GamesA: 4, GamesB: 0),
            (Score: "5-0 0-0", GamesA: 5, GamesB: 0),
            (Score: "5-1 0-0", GamesA: 5, GamesB: 1),
            (Score: "5-2 0-0", GamesA: 5, GamesB: 2)
        };

        foreach (var step in gamePlan)
        {
            await BroadcastScoreAsync(Court2Id, Court2MatchId, "L. Djere", "D. Lajovic", step.Score, ct);

            await MaybeBreakAfterGameAsync(
                Court2Id,
                gamesInSet: step.GamesA + step.GamesB,
                isSetFinished: false,
                ct,
                preDelayOverrideMs: preAdsDelay,
                changeoverOverrideMs: changeoverDuration);

            if (!ct.IsCancellationRequested) await Task.Delay(_tickDelay, ct);
        }

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(_tickDelay, ct);
            await BroadcastScoreAsync(Court2Id, Court2MatchId, "L. Djere", "D. Lajovic", "5-2 0-0", ct);
        }
    }
}
