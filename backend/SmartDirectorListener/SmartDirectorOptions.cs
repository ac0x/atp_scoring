namespace Backend.SmartDirectorListener;

public class SmartDirectorOptions
{
    public string BindAddress { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 33211;
    public int BufferSize { get; set; } = 8192;
    public int FrameFlushThresholdMs { get; set; } = 180;
    public int PollDelayMs { get; set; } = 10;
    public int ReconnectDelaySeconds { get; set; } = 5;
}