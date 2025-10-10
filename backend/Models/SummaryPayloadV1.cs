using System;
using System.Collections.Generic;

namespace Backend.Models;

public sealed record FinishedMatchV1(
    string Players,
    IReadOnlyList<string> Sets,
    DateTimeOffset CompletedAt
);

public sealed record UpcomingMatchV1(
    string Court,
    string Players
);

public sealed record SummaryPayloadV1(
    string CourtId,
    IReadOnlyList<FinishedMatchV1> Finished,
    IReadOnlyList<UpcomingMatchV1> Upcoming,
    DateTimeOffset ServerTimeUtc
);