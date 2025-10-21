import { API_BASE } from '@/config'
import type { Court, MatchSummary } from '@/types'

const BASE = (API_BASE ?? '').replace(/\/$/, '')
const RETRY_ATTEMPTS = 2
const RETRY_DELAY_MS = 300

async function fetchWithRetry<T>(path: string): Promise<T> {
  let lastError: unknown

  for (let attempt = 0; attempt < RETRY_ATTEMPTS; attempt++) {
    try {
      const response = await fetch(`${BASE}${path}`)
      if (!response.ok) {
        throw new Error(`Request failed (${response.status})`)
      }
      return response.json() as Promise<T>
    } catch (error) {
      lastError = error
      if (attempt < RETRY_ATTEMPTS - 1) {
        await new Promise((resolve) => setTimeout(resolve, RETRY_DELAY_MS))
      }
    }
  }

  throw lastError instanceof Error
    ? lastError
    : new Error(String(lastError ?? 'Request failed'))
}

export async function fetchCourts(): Promise<Court[]> {
  return fetchWithRetry<Court[]>('/api/courts')
}

export async function fetchMatches(): Promise<MatchSummary[]> {
  return fetchWithRetry<MatchSummary[]>('/api/matches')
}
