export type Court = { courtId: string; name: string }
export type MatchSummary = { matchId: string; courtId: string; playerA: string; playerB: string }
export type ScorePayload = {
  MatchId: string; CourtId: string; PlayerA: string; PlayerB: string;
  Score: string;  ServerTimeUtc: string;
}
