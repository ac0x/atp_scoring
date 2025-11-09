using System.Collections.Generic;

namespace Backend.SmartDirectorListener.Models;

public class LiveSnapshotMessage
{
    public required string TeamA { get; init; }
    public required string TeamB { get; init; }
    public required IReadOnlyList<string> Sets { get; init; }
    public required string Points { get; init; }
    public required string Server { get; init; }
    public required string Clock { get; init; }
}