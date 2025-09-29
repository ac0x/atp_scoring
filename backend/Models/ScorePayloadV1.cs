namespace Backend.Models;
public sealed record ScorePayloadV1(
    string MatchId,
    string CourtId,
    string PlayerA,
    string PlayerB,
    string Score,
    DateTimeOffset ServerTimeUtc
);
