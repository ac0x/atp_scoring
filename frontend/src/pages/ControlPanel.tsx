import { useCallback, useEffect, useMemo, useState } from 'react'
import { useSignalR } from '@/hooks/useSignalR'
import { fetchCourts, fetchMatches } from '@/api'
import { useScoreStore } from '@/stores/useScoreStore'

const statusColors: Record<string, string> = {
  connected: '#2ecc71',
  connecting: '#f1c40f',
  reconnecting: '#f39c12',
  disconnected: '#c0392b',
  error: '#e74c3c',
}

export default function ControlPanel() {
  useSignalR()
  const {
    courts,
    matches,
    setCourts,
    setMatches,
    connectionStatus,
    lastPacketAt,
    lastError,
  } = useScoreStore()

  const [courtsError, setCourtsError] = useState<string | undefined>()
  const [matchesError, setMatchesError] = useState<string | undefined>()

  const loadCourts = useCallback(async () => {
    try {
      const data = await fetchCourts()
      setCourts(data)
      setCourtsError(undefined)
    } catch (error) {
      console.error('Failed to load courts', error)
      setCourtsError('Failed to load courts.')
    }
  }, [setCourts])

  const loadMatches = useCallback(async () => {
    try {
      const data = await fetchMatches()
      setMatches(data)
      setMatchesError(undefined)
    } catch (error) {
      console.error('Failed to load matches', error)
      setMatchesError('Failed to load matches.')
    }
  }, [setMatches])

  useEffect(() => {
    loadCourts()
    loadMatches()
  }, [loadCourts, loadMatches])

  const formattedPacketTime = useMemo(() => {
    if (!lastPacketAt) return '—'
    return new Date(lastPacketAt).toLocaleTimeString(undefined, {
      hour12: false,
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    })
  }, [lastPacketAt])

  const openOverlay = (courtId: string) => {
    const url = `${location.origin}/overlay/court/${courtId}?autoFs=1`
    window.open(url, '_blank', 'popup=yes,noopener,noreferrer,resizable=yes,scrollbars=no')
  }

  const statusColor = statusColors[connectionStatus] ?? '#7f8c8d'

  return (
    <div style={{ padding: 16 }}>
      <h1>Control Panel (DEMO)</h1>

      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 12,
          padding: '8px 12px',
          borderRadius: 999,
          background: 'rgba(0,0,0,0.05)',
          width: 'fit-content',
        }}
      >
        <span
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            padding: '4px 12px',
            borderRadius: 999,
            background: statusColor,
            color: '#fff',
            fontSize: 12,
            letterSpacing: '0.1em',
            textTransform: 'uppercase',
            minWidth: 90,
          }}
        >
          {connectionStatus}
        </span>
        <span style={{ fontSize: 12, color: '#2c3e50', letterSpacing: '0.08em' }}>
          Last packet: {formattedPacketTime}
        </span>
        {lastError && (
          <span
            title={lastError}
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              width: 20,
              height: 20,
              borderRadius: '50%',
              background: '#e74c3c',
              color: '#fff',
              fontWeight: 700,
              cursor: 'help',
            }}
          >
            !
          </span>
        )}
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginTop: 16 }}>
        <section>
          <h3>Courts</h3>
          {courtsError && (
            <div
              style={{
                marginBottom: 8,
                padding: '8px 12px',
                borderRadius: 8,
                background: 'rgba(192, 57, 43, 0.1)',
                color: '#c0392b',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                gap: 12,
                fontSize: 14,
              }}
            >
              <span>Failed to load courts.</span>
              <button onClick={loadCourts}>Retry</button>
            </div>
          )}
          <ul>
            {courts.map((c) => (
              <li key={c.courtId} style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                <span>
                  [{c.courtId}] {c.name}
                </span>
                <button onClick={() => openOverlay(c.courtId)}>Open overlay</button>
              </li>
            ))}
          </ul>
        </section>

        <section>
          <h3>Matches</h3>
          {matchesError && (
            <div
              style={{
                marginBottom: 8,
                padding: '8px 12px',
                borderRadius: 8,
                background: 'rgba(192, 57, 43, 0.1)',
                color: '#c0392b',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                gap: 12,
                fontSize: 14,
              }}
            >
              <span>Failed to load matches.</span>
              <button onClick={loadMatches}>Retry</button>
            </div>
          )}
          <ul>
            {matches.map((m) => (
              <li key={m.matchId}>
                [{m.courtId}] {m.playerA} vs {m.playerB}
              </li>
            ))}
          </ul>
        </section>
      </div>
    </div>
  )
}
