using Microsoft.AspNetCore.SignalR;
using Backend.Models;

namespace Backend.Hubs;

public class ScoreHub : Hub
{
    public async Task BroadcastScore(ScorePayloadV1 payload)
        => await Clients.All.SendAsync("ScoreUpdate", payload);

    public async Task BroadcastScene(SceneSwitchPayloadV1 payload)
        => await Clients.All.SendAsync("SceneSwitch", payload);
}


