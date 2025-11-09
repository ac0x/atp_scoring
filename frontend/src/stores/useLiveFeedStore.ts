import { create } from 'zustand'
import type { LiveSnapshotPayload } from '@/types'

type Conn = 'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'error'

type LiveSnapshot = LiveSnapshotPayload & { receivedAt: number }

type State = {
  status: Conn
  error?: string
  snapshot?: LiveSnapshot
  history: LiveSnapshot[]
  lastReceived?: number
  setStatus: (status: Conn) => void
  setError: (error?: string) => void
  pushSnapshot: (payload: LiveSnapshotPayload) => void
}

const MAX_HISTORY = 10

export const useLiveFeedStore = create<State>((set) => ({
  status: 'disconnected',
  history: [],

  setStatus: (status) => set({ status }),
  setError: (error) => set({ error }),

  pushSnapshot: (payload) =>
    set((state) => {
      const receivedAt = Date.now()
      const next: LiveSnapshot = { ...payload, receivedAt }
      const history = state.history.length > 0 && areSnapshotsEqual(state.history[0], next)
        ? state.history
        : [next, ...state.history].slice(0, MAX_HISTORY)

      return {
        snapshot: next,
        history,
        lastReceived: receivedAt,
        error: undefined,
      }
    }),
}))

function areSnapshotsEqual(a: LiveSnapshot, b: LiveSnapshot) {
  return (
    a.teamA === b.teamA &&
    a.teamB === b.teamB &&
    a.points === b.points &&
    a.server === b.server &&
    a.clock === b.clock &&
    arraysEqual(a.sets, b.sets)
  )
}

function arraysEqual(left: string[], right: string[]) {
  if (left.length !== right.length) return false
  return left.every((value, index) => value === right[index])
}