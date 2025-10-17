using System;

namespace Backend.Models;

public sealed record PlayerCardV1(
    string Name,
    string Country,
    int Rank,
    int Age,
    int Titles
);

public sealed record H2HRecordV1(
    string PlayerA,
    string PlayerB,
    int WinsA,
    int WinsB,
    string LastMeeting
);

public sealed record AnnounceNextPayloadV1(
    string CourtId,
    string Step,
    PlayerCardV1? Player,
    H2HRecordV1? H2H,
    DateTimeOffset ServerTimeUtc
);