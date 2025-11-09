import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr'
import { useEffect, useRef } from 'react'
import { LIVE_HUB_URL } from '@/config'
import { useLiveFeedStore } from '@/stores/useLiveFeedStore'
import type { LiveSnapshotPayload } from '@/types'

const RECONNECT_DELAYS = [0, 2000, 5000, 10000, 30000]

export function useLiveFeed() {
  const connectionRef = useRef<HubConnection | null>(null)
  const setStatus = useLiveFeedStore((state) => state.setStatus)
  const setError = useLiveFeedStore((state) => state.setError)
  const pushSnapshot = useLiveFeedStore((state) => state.pushSnapshot)

  useEffect(() => {
    let mounted = true
    const url = (LIVE_HUB_URL ?? '/live') as string
    const connection = new HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect(RECONNECT_DELAYS)
      .configureLogging(LogLevel.Information)
      .build()

    connectionRef.current = connection

    const handleSnapshot = (payload: LiveSnapshotPayload) => {
      pushSnapshot(payload)
    }

    connection.on('Snapshot', handleSnapshot)
    connection.on('snapshot', handleSnapshot)

    connection.onreconnecting((error) => {
      setStatus('reconnecting')
      setError(error?.message)
    })

    connection.onreconnected(() => {
      setStatus('connected')
      setError(undefined)
    })

    connection.onclose((error) => {
      setStatus('disconnected')
      setError(error?.message)
    })

    ;(async () => {
      try {
        setStatus('connecting')
        await connection.start()
        if (!mounted) return
        setStatus('connected')
        setError(undefined)
      } catch (error: any) {
        if (!mounted) return
        setStatus('error')
        setError(error?.message ?? String(error))
      }
    })()

    return () => {
      mounted = false
      connection.stop().catch(() => {})
      connectionRef.current = null
    }
  }, [setStatus, setError, pushSnapshot])
}