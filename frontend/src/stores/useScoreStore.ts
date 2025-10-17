import { create } from 'zustand'
import type { Court, MatchSummary, ScorePayload, SummaryData, AnnounceNextPayload } from '@/types'

type Conn = 'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'error'

type State = {
  connectionStatus: Conn
  lastPacketAt?: number
  lastError?: string
  courts: Court[]
  matches: MatchSummary[]
  byMatch: Record<string, ScorePayload>
  byCourt: Record<string, ScorePayload> // poslednji update po terenu
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
      // Ako imamo stariji paket za isti court, zameni samo ako je novi svežiji
      const ok =
        !current ||
        new Date(p.ServerTimeUtc).getTime() >= new Date(current.ServerTimeUtc).getTime()
      if (ok) next[p.CourtId] = p
      return {
        byMatch: { ...st.byMatch, [p.MatchId]: p },
        byCourt: next,
        lastPacketAt: Date.now(),
      }
    }),
}))
