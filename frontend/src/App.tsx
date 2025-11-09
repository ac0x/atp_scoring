import './App.css'
import { useMemo } from 'react'
import { useLiveFeed } from '@/hooks/useLiveFeed'
import { useLiveFeedStore } from '@/stores/useLiveFeedStore'
import type { LiveSnapshot } from '@/types'

function formatTime(ts?: number | string | null): string {
  if (!ts) return '—'
  const d = new Date(ts)
  return d.toLocaleTimeString(undefined, {
    hour12: false,
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  })
}

function App() {
  useLiveFeed()

  const snapshot = useLiveFeedStore((s) => s.snapshot)
  const history = useLiveFeedStore((s) => s.history)
  const status = useLiveFeedStore((s) => s.status)
  const lastReceived = useLiveFeedStore((s) => s.lastReceived)
  const error = useLiveFeedStore((s) => s.error)

  const scoreLine = useMemo(() => {
    const sets = snapshot?.sets ?? []
    return sets.length > 0 ? sets.join(', ') : '—'
  }, [snapshot?.sets])

  const serverLabel = useMemo(() => {
    const s = snapshot?.server?.toUpperCase()
    if (!s) return '—'
    if (s === 'A') return `A (${snapshot?.teamA ?? '—'})`
    if (s === 'B') return `B (${snapshot?.teamB ?? '—'})`
    return snapshot?.server ?? '—'
  }, [snapshot?.server, snapshot?.teamA, snapshot?.teamB])

  const lastUpdatedText = useMemo(() => formatTime(lastReceived), [lastReceived])

  return (
    <div className="app">
      <header className="header">
        <h1>ATP SmartDirector Preview</h1>
      </header>

      <main className="content">
        <section className="panel live-panel">
          <h2>Trenutni meč</h2>

          {snapshot ? (
            <div className="match-card">
              <div className="match-header">
                <span className="players">
                  {snapshot.teamA} vs {snapshot.teamB}
                </span>
                <span className="court">SMARTDIRECTOR</span>
              </div>

              <div className="score-line">
                <span>Rezultat setova:</span>
                <span className="score-values">{scoreLine}</span>
              </div>

              <div className="points-remaining">
                Trenutni gem: <strong>{snapshot.points || '—'}</strong>
              </div>

              <div className="events">
                <h3>Live detalji</h3>
                <ul>
                  <li>
                    <span className="event-index">Server:</span>
                    <span className="event-description">{serverLabel}</span>
                  </li>
                  <li>
                    <span className="event-index">Clock:</span>
                    <span className="event-description">{snapshot.clock || '—'}</span>
                  </li>
                  <li>
                    <span className="event-index">Last update:</span>
                    <span className="event-description">{lastUpdatedText}</span>
                  </li>
                </ul>
              </div>
            </div>
          ) : (
            <div className="waiting-message">Waiting for SmartDirector feed…</div>
          )}
        </section>

        <section className="panel summary-panel">
          <h2>Live feed history</h2>

          {history.length > 0 ? (
            <ul className="finished-list">
              {history.map((entry: LiveSnapshot) => (
                <li key={entry.receivedAt ?? `${entry.teamA}-${entry.teamB}-${Math.random()}`} className="finished-item">
                  <div className="finished-players">
                    {entry.teamA} vs {entry.teamB}
                  </div>
                  <div className="finished-score">
                    {(entry.sets?.length ? entry.sets.join(' ') : '—') + ' · ' + (entry.points || '—')}
                  </div>
                </li>
              ))}
            </ul>
          ) : (
            <div className="waiting-message small">No frames received yet.</div>
          )}

          <div className="upcoming">
            <h3>Status</h3>
            <ul>
              <li className="upcoming-item">
                <span className="upcoming-court">Connection</span>
                <span className="upcoming-players">{status}</span>
              </li>
              <li className="upcoming-item">
                <span className="upcoming-court">Last frame</span>
                <span className="upcoming-players">{lastUpdatedText}</span>
              </li>
              {error && (
                <li className="upcoming-item">
                  <span className="upcoming-court">Last error</span>
                  <span className="upcoming-players error-text">{error}</span>
                </li>
              )}
            </ul>
          </div>
        </section>
      </main>
    </div>
  )
}

export default App
