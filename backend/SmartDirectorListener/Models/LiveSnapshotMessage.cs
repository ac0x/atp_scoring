using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Backend.SmartDirectorListener.Models;

public class LiveSnapshotMessage
{
    [JsonPropertyName("teamA")]
    public required string TeamA { get; init; }

    [JsonPropertyName("teamB")]
    public required string TeamB { get; init; }

    [JsonPropertyName("sets")]
    public required IReadOnlyList<string> Sets { get; init; }

    [JsonPropertyName("points")]
    public required string Points { get; init; }

    [JsonPropertyName("server")]
    public required string Server { get; init; }

    [JsonPropertyName("clock")]
    public required string Clock { get; init; }
}