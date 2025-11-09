using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

public class ScoreHub : Hub
{
    private readonly TickerHostedService _ticker;

    public ScoreHub(TickerHostedService ticker)
    {
        _ticker = ticker;
    }

    public async Task BroadcastScore(ScorePayloadV1 payload)
        => await Clients.All.SendAsync("ScoreUpdate", payload);

    public async Task BroadcastScene(SceneSwitchPayloadV1 payload)
        => await Clients.All.SendAsync("SceneSwitch", payload);

    public async Task BroadcastAnnounceNext(AnnounceNextPayloadV1 payload)
        => await Clients.All.SendAsync("AnnounceNext", payload);

    public Task SetHold(string courtId, string reason)
        => _ticker.SetManualHoldAsync(courtId, reason);

    public Task ClearHold(string courtId)
        => _ticker.ClearManualHoldAsync(courtId);
}
