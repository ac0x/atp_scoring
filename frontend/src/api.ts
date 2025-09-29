import type { Court, MatchSummary } from '@/types'
const BASE = import.meta.env.VITE_API_BASE as string
export async function fetchCourts(): Promise<Court[]> {
  const r = await fetch(`${BASE}/api/courts`); if(!r.ok) throw new Error('courts'); return r.json()
}
export async function fetchMatches(): Promise<MatchSummary[]> {
  const r = await fetch(`${BASE}/api/matches`); if(!r.ok) throw new Error('matches'); return r.json()
}
