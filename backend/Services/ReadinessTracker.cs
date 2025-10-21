namespace Backend.Services;

public class ReadinessTracker
{
    private volatile bool _hubReady;
    private volatile bool _tickerStarted;

    public bool HubReady => _hubReady;
    public bool TickerStarted => _tickerStarted;
    public bool IsReady => _hubReady && _tickerStarted;

    public void MarkHubReady() => _hubReady = true;
    public void MarkTickerStarted() => _tickerStarted = true;
}