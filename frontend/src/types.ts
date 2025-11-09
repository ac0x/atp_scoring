export type Court = { courtId: string; name: string }
export type MatchSummary = { matchId: string; courtId: string; playerA: string; playerB: string }

export type ScorePayload = {
  MatchId: string
  CourtId: string
  PlayerA: string
  PlayerB: string
  Score: string
  ServerTimeUtc: string
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

export type PlayerCard = {
  Name: string
  Country: string
  Rank: number
  Age: number
  Titles: number
}

export type H2HRecord = {
  PlayerA: string
  PlayerB: string
  WinsA: number
  WinsB: number
  LastMeeting: string
}

export type AnnounceNextPayload = {
  CourtId: string
  Step: string
  Player?: PlayerCard
  H2H?: H2HRecord
  ServerTimeUtc: string
}

export type LiveSnapshotPayload = {
  teamA: string
  teamB: string
  sets: string[]
  points: string
  server: string
  clock: string
}