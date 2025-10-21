namespace Backend.Services;

public class TickerOptions
{
    public int TickMs { get; set; } = 2000;
    public AnnounceOptions Announce { get; set; } = new();

    public class AnnounceOptions
    {
        public int FedererMs { get; set; } = 5000;
        public int NadalMs { get; set; } = 5000;
        public int H2HMs { get; set; } = 6000;
        public int AdsMs { get; set; } = 5000;
    }
}