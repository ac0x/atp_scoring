import { HubConnectionBuilder, LogLevel, HubConnection } from '@microsoft/signalr'
import { useEffect, useRef } from 'react'
import { useScoreStore } from '@/stores/useScoreStore'
import type { ScorePayload, SummaryUpdate } from '@/types'

export function useSignalR() {
  const url = import.meta.env.VITE_HUB_URL as string
  const connRef = useRef<HubConnection | null>(null)

  const setStatus  = useScoreStore(s => s.setStatus)
  const setError   = useScoreStore(s => s.setError)
  const upsert     = useScoreStore(s => s.upsert)
  const setScene   = useScoreStore(s => s.setScene)
  const setSummary = useScoreStore(s => s.setSummary)

  useEffect(() => {
    let mounted = true

    const conn = new HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Information)
      .build()
    connRef.current = conn

    // score tick
    conn.on('ScoreUpdate', (p: ScorePayload) => upsert(p))

    // scene switch (case-insensitive)
    const onScene = (p: { CourtId: string; Scene: string; ServerTimeUtc: string }) =>
      setScene(p.CourtId, p.Scene)
    conn.on('SceneSwitch', onScene)
    conn.on('sceneswitch', onScene)

    // summary update (finished + upcoming)
    const onSummary = (p: SummaryUpdate) => {
      setSummary(p.CourtId, {
        finished: p.Finished.map(f => ({ players: f.Players, sets: f.Sets })),
        upcoming: p.Upcoming.map(u => ({ court: u.Court, players: u.Players })),
        serverTimeUtc: new Date(p.ServerTimeUtc).getTime(),
      })
    }
    conn.on('SummaryUpdate', onSummary)
    conn.on('summaryupdate', onSummary)

    // lifecycle
    conn.onreconnecting(e => { setStatus('reconnecting'); setError(e?.message) })
    conn.onreconnected(() => { setStatus('connected'); setError(undefined) })
    conn.onclose(e => { setStatus('disconnected'); setError(e?.message) })

    ;(async () => {
      try {
        setStatus('connecting')
        await conn.start()
        if (!mounted) return
        setStatus('connected')
      } catch (e: any) {
        if (!mounted) return
        setStatus('error')
        setError(e?.message ?? String(e))
      }
    })()

    return () => {
      mounted = false
      conn.stop().catch(() => {})
      connRef.current = null
    }
  }, [url, setStatus, setError, upsert, setScene, setSummary])

  return {
    invoke: async (method: string, ...args: any[]) => {
      if (!connRef.current) throw new Error('Connection not ready')
      return connRef.current.invoke(method, ...args)
    }
  }
}
