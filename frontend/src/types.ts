export type Court = { courtId: string; name: string }
export type MatchSummary = { matchId: string; courtId: string; playerA: string; playerB: string }
export type ScorePayload = {
  MatchId: string; CourtId: string; PlayerA: string; PlayerB: string;
  Score: string;  ServerTimeUtc: string;
}

export type SummaryUpdate = {
  CourtId: string
  Finished: { Players: string; Sets: string[]; CompletedAt: string }[]
  Upcoming: { Court: string; Players: string }[]
  ServerTimeUtc: string
}

export type SummaryData = {
  finished: { players: string; sets: string[] }[]
  upcoming: { court: string; players: string }[]
  serverTimeUtc: number
}