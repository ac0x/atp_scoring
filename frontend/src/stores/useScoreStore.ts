import { create } from 'zustand'
import type { Court, MatchSummary, ScorePayload, SummaryData, AnnounceNextPayload } from '@/types'
import { isFresher } from '@/utils/time'

const MAX_CLOCK_SKEW_MS = 60 * 60 * 1000

type Conn = 'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'error'

type State = {
  connectionStatus: Conn
  lastPacketAt?: number
  lastError?: string
  courts: Court[]
  matches: MatchSummary[]
  byMatch: Record<string, ScorePayload>
  byCourt: Record<string, ScorePayload>
  sceneByCourt: Record<string, string>
  summaryByCourt: Record<string, SummaryData>
  announceByCourt: Record<string, AnnounceNextPayload | undefined>
  setStatus: (s: Conn) => void
  setError: (e?: string) => void
  setCourts: (c: Court[]) => void
  setMatches: (m: MatchSummary[]) => void
  setScene: (courtId: string, scene: string) => void
  setSummary: (courtId: string, summary: SummaryData) => void
  setAnnounce: (courtId: string, payload: AnnounceNextPayload | undefined) => void
  upsert: (p: ScorePayload) => void
}

export const useScoreStore = create<State>((set) => ({
  connectionStatus: 'disconnected',
  courts: [],
  matches: [],
  byMatch: {},
  byCourt: {},
  sceneByCourt: {},
  summaryByCourt: {},
  announceByCourt: {},

  setStatus: (s) => set({ connectionStatus: s }),
  setError: (e) => set({ lastError: e }),
  setCourts: (c) => set({ courts: c }),
  setMatches: (m) => set({ matches: m }),

  setScene: (courtId, scene) =>
    set((st) => ({ sceneByCourt: { ...st.sceneByCourt, [courtId]: scene } })),

  setSummary: (courtId, summary) =>
    set((st) => ({ summaryByCourt: { ...st.summaryByCourt, [courtId]: summary } })),

  setAnnounce: (courtId, payload) =>
    set((st) => ({ announceByCourt: { ...st.announceByCourt, [courtId]: payload } })),

  upsert: (p) =>
    set((st) => {
      const next = { ...st.byCourt }
      const current = st.byCourt[p.CourtId]
      const accept = !current || isFresher(p.ServerTimeUtc, current.ServerTimeUtc)

      const serverTimeMs = Date.parse(p.ServerTimeUtc)
      if (!Number.isNaN(serverTimeMs) && Math.abs(serverTimeMs - Date.now()) > MAX_CLOCK_SKEW_MS) {
        console.warn(
          '[score-store] Large clock skew detected for court %s (payload=%s, now=%s)',
          p.CourtId,
          p.ServerTimeUtc,
          new Date().toISOString()
        )
      }

      if (accept) {
        next[p.CourtId] = p
      }

      return {
        byMatch: { ...st.byMatch, [p.MatchId]: p },
        byCourt: next,
        lastPacketAt: Date.now(),
      }
    }),
}))
