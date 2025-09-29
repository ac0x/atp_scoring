namespace Backend.Models;

public sealed record SceneSwitchPayloadV1(
    string CourtId,
    string Scene, // npr. "ADS" ili "LIVE"
    DateTimeOffset ServerTimeUtc
);
