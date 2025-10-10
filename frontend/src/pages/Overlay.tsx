import { useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useSignalR } from '@/hooks/useSignalR'
import { useScoreStore } from '@/stores/useScoreStore'

export default function Overlay() {
  useSignalR()
  const { courtId } = useParams()
  const payload = useScoreStore((s) => (courtId ? s.byCourt[courtId] : undefined))
  const scene = useScoreStore((s) => (courtId ? s.sceneByCourt[courtId] : undefined))
  const summary = useScoreStore((s) => (courtId ? s.summaryByCourt[courtId] : undefined))

  useEffect(() => {
    const p = new URLSearchParams(location.search)
    if (p.get('autoFs') === '1') {
      setTimeout(async () => {
        try {
          await document.documentElement.requestFullscreen()
        } catch {}
      }, 50)
    }
  }, [])

  // Ako je scena ADS, prikaži "REKLAME"
  if (scene === 'ADS') {
    return (
      <div
        style={{
          width: '100vw',
          height: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          background: '#000',
          color: '#fff',
          fontSize: 56,
          fontWeight: 800,
          letterSpacing: 2,
        }}
      >
        REKLAME
      </div>
    )
  }

  if (scene === 'SUMMARY') {
    return (
      <div
        style={{
          width: '100vw',
          height: '100vh',
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          background: 'radial-gradient(circle at top, #0b1d33 0%, #030912 80%)',
          color: '#f2f5f8',
          fontFamily: 'system-ui',
        }}
      >
        <div
          style={{
            width: 'min(960px, 90vw)',
            padding: '48px 64px',
            borderRadius: 24,
            background: 'rgba(8, 24, 40, 0.92)',
            boxShadow: '0 20px 60px rgba(0,0,0,0.45)',
            border: '1px solid rgba(143, 215, 255, 0.2)',
          }}
        >
          <h1
            style={{
              margin: 0,
              marginBottom: 32,
              fontSize: 32,
              letterSpacing: '0.12em',
              textTransform: 'uppercase',
              color: '#ffd166',
              textAlign: 'center',
            }}
          >
            GOTOVI MEČEVI:
          </h1>

          <ol
            style={{
              listStyle: 'none',
              padding: 0,
              margin: 0,
              display: 'flex',
              flexDirection: 'column',
              gap: 16,
            }}
          >
            {(summary?.finished ?? []).map((match, idx) => (
              <li
                key={`${match.players}-${idx}`}
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  gap: 16,
                  padding: '18px 24px',
                  borderRadius: 16,
                  background: 'rgba(255,255,255,0.05)',
                  border: '1px solid rgba(255,255,255,0.08)',
                }}
              >
                <span style={{ fontWeight: 600, fontSize: 20 }}>{match.players}</span>
                <span style={{ fontSize: 18, letterSpacing: '0.08em' }}>{match.sets.join(' ')}</span>
              </li>
            ))}
          </ol>

          <div style={{ marginTop: 40 }}>
            <h2
              style={{
                margin: 0,
                marginBottom: 20,
                fontSize: 24,
                letterSpacing: '0.08em',
                color: '#8fd7ff',
                textTransform: 'uppercase',
              }}
            >
              SLEDEĆI MEČEVI:
            </h2>

            <ul
              style={{
                listStyle: 'none',
                padding: 0,
                margin: 0,
                display: 'flex',
                flexDirection: 'column',
                gap: 12,
              }}
            >
              {(summary?.upcoming ?? []).map((match) => (
                <li
                  key={`${match.court}-${match.players}`}
                  style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    gap: 16,
                    padding: '14px 20px',
                    borderRadius: 14,
                    background: 'rgba(143, 215, 255, 0.12)',
                    border: '1px solid rgba(143, 215, 255, 0.25)',
                  }}
                >
                  <span style={{ fontWeight: 700, letterSpacing: '0.06em' }}>{match.court}</span>
                  <span style={{ fontSize: 18 }}>{match.players}</span>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div style={{ padding: 16, fontFamily: 'system-ui' }}>
      <div style={{ marginBottom: 8 }}>
        <Link to="/">{'← back'}</Link>
      </div>
      <h2>Overlay (Court {courtId})</h2>
      {!payload ? (
        <div>No live data yet for court {courtId}</div>
      ) : (
        <div style={{ fontSize: 28 }}>
          <div>
            <b>{payload.PlayerA}</b> vs <b>{payload.PlayerB}</b>
          </div>
          <div style={{ marginTop: 8 }}>Score: {payload.Score}</div>
          <div style={{ marginTop: 4, fontSize: 12, color: '#666' }}>
            Court: {payload.CourtId} | Server: {new Date(payload.ServerTimeUtc).toLocaleTimeString()}
          </div>
        </div>
      )}
    </div>
  )
}
