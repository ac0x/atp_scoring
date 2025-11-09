namespace Backend.Services;

public class TickerOptions
{
    private int _tickMs = 2000;

    public int DefaultTickMs
    {
        get => _tickMs;
        set => _tickMs = value;
    }

    public int TickMs
    {
        get => _tickMs;
        set => _tickMs = value;
    }

    public int PreAdsDelayMs { get; set; } = 5000;
    public int ChangeoverAdsMs { get; set; } = 60_000;
    public int SetBreakAdsMs { get; set; } = 120_000;
    public int ManualAdsMaxMs { get; set; } = 600_000;

    public CourtOptions C2 { get; set; } = new();

    public AnnounceOptions Announce { get; set; } = new();

    public class AnnounceOptions
    {
        public int FedererMs { get; set; } = 5000;
        public int NadalMs { get; set; } = 5000;
        public int H2HMs { get; set; } = 6000;
        public int AdsMs { get; set; } = 5000;
    }

    public class CourtOptions
    {
        public int PreAdsDelayMs { get; set; } = 5000;
        public int PostGemAdsMs { get; set; } = 45_000;
    }
}