import { useEffect, useMemo, useRef, useState, type CSSProperties } from 'react'
import { useParams } from 'react-router-dom'
import { useSignalR } from '@/hooks/useSignalR'
import { useScoreStore } from '@/stores/useScoreStore'

const adVideoModules = import.meta.glob<{ default: string }>('@/assets/ads45.mp4', {
  eager: true,
})

const adVideoFromAssets = Object.values(adVideoModules)[0]?.default
const fallbackAdVideo = `${import.meta.env.BASE_URL}ads45.mp4`
const AD_VIDEO_SRC = adVideoFromAssets ?? fallbackAdVideo

export default function Overlay() {
  useSignalR()
  const { courtId } = useParams()
  const payload = useScoreStore((s) => (courtId ? s.byCourt[courtId] : undefined))
  const scene = useScoreStore((s) => (courtId ? s.sceneByCourt[courtId] : undefined))
  const summary = useScoreStore((s) => (courtId ? s.summaryByCourt[courtId] : undefined))

  const [adFailed, setAdFailed] = useState(false)
  const adRef = useRef<HTMLVideoElement | null>(null)

  const scoreDetails = useMemo(() => {
    if (!payload) return undefined
    const tokens = payload.Score.split(/\s+/).filter(Boolean)
    if (tokens.length === 0) {
      return { sets: [], currentGame: undefined, isFinal: false, finalLine: '' }
    }

    const isFinal = tokens[tokens.length - 1].toUpperCase() === 'FINAL'
    const workingTokens = isFinal ? tokens.slice(0, -1) : tokens.slice()
    let currentGameToken: string | undefined

    if (!isFinal && workingTokens.length > 0) {
      currentGameToken = workingTokens[workingTokens.length - 1]
      workingTokens.pop()
    }

    const parsePair = (token: string) => {
      const [left, right] = token.split(/[-:]/)
      return { playerA: (left ?? '').trim(), playerB: (right ?? '').trim() }
    }

    const sets = workingTokens
      .filter((token) => /[-:]/.test(token))
      .map((token, index) => {
        const pair = parsePair(token)
        return {
          id: `set-${index + 1}`,
          index,
          display: `${pair.playerA}:${pair.playerB}`.replace(/^:+|:+$/g, ''),
          ...pair,
        }
      })

    const currentGame =
      currentGameToken && /[-:]/.test(currentGameToken)
        ? { display: currentGameToken, ...parsePair(currentGameToken) }
        : undefined

    const finalLineRaw = sets.map((s) => `${s.playerA}:${s.playerB}`).join(' ').trim()
    const finalLine = finalLineRaw.length > 0 ? finalLineRaw : 'FINAL'

    return { sets, currentGame, isFinal, finalLine }
  }, [payload])

  useEffect(() => {
    const p = new URLSearchParams(location.search)
    if (p.get('autoFs') === '1') {
      setTimeout(async () => {
        try { await document.documentElement.requestFullscreen() } catch {}
      }, 50)
    }
  }, [])

  useEffect(() => {
    if (scene === 'ADS') {
      setAdFailed(false)
      const video = adRef.current
      if (video) {
        video.currentTime = 0
        const playPromise = video.play()
        if (playPromise) playPromise.catch(() => setAdFailed(true))
      }
    }
  }, [scene])

  if (scene === 'ADS') {
    return (
      <div style={{
        position: 'relative', width: '100vw', height: '100vh', overflow: 'hidden',
        background: '#000', display: 'flex', alignItems: 'center', justifyContent: 'center',
      }}>
        {AD_VIDEO_SRC && !adFailed ? (
          <video
            key={AD_VIDEO_SRC}
            ref={adRef}
            src={AD_VIDEO_SRC}
            muted
            playsInline
            autoPlay
            preload="auto"
            style={{
               position: 'absolute',
               inset: 0,
               width: '100%',
               height: '100%',
               //objectFit: 'cover',
                // Fit to screen bez kropljenja (letterbox/pillarbox po potrebi)
               objectFit: 'contain',
               zIndex: 0,
             }}
            onError={() => setAdFailed(true)}
          />
        ) : (
          <div style={{
            position: 'absolute', inset: 0, display: 'flex', alignItems: 'center', justifyContent: 'center',
            background: 'radial-gradient(circle at center, rgba(255, 209, 102, 0.3), rgba(0, 0, 0, 0.95))',
            color: '#ffd166', fontFamily: 'system-ui', fontSize: 56, fontWeight: 800, letterSpacing: '0.2em', zIndex: 2,
          }}>
            REKLAME
          </div>
        )}
        <div style={{
          position: 'absolute', inset: 0, background: 'linear-gradient(135deg, rgba(5, 18, 34, 0.15), rgba(3, 9, 18, 0.65))',
          pointerEvents: 'none', zIndex: 1,
        }} />
      </div>
    )
  }

  const screenStyle: CSSProperties = {
    width: '100vw',
    height: '100vh',
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    background: 'radial-gradient(circle at top, #0b1d33 0%, #030912 80%)',
    color: '#f2f5f8',
    fontFamily: 'system-ui',
  }

  const cardStyle: CSSProperties = {
    width: 'min(960px, 90vw)',
    padding: '48px 64px',
    borderRadius: 24,
    background: 'rgba(8, 24, 40, 0.92)',
    boxShadow: '0 20px 60px rgba(0,0,0,0.45)',
    border: '1px solid rgba(143, 215, 255, 0.2)',
  }

  if (scene === 'FINISHED') {
    return (
      <div style={screenStyle}>
        <div style={cardStyle}>
          <h1 style={{ margin: 0, marginBottom: 32, fontSize: 32, letterSpacing: '0.12em', textTransform: 'uppercase', color: '#ffd166', textAlign: 'center' as const }}>
            GOTOVI MEČEVI
          </h1>

          {summary?.finished?.length ? (
            <ol style={{ listStyle: 'none', padding: 0, margin: 0, display: 'flex', flexDirection: 'column', gap: 16 }}>
              {summary.finished.map((m, idx) => (
                <li key={`${m.players}-${idx}`} style={{
                  display: 'flex', justifyContent: 'space-between', gap: 16, padding: '18px 24px',
                  borderRadius: 16, background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.08)',
                }}>
                  <span style={{ fontWeight: 600, fontSize: 20 }}>{m.players}</span>
                  <span style={{ fontSize: 18, letterSpacing: '0.08em' }}>{m.sets.join(' ')}</span>
                </li>
              ))}
            </ol>
          ) : (
            <div style={{ textAlign: 'center' as const, fontSize: 20 }}>Nema završenih mečeva.</div>
          )}
        </div>
      </div>
    )
  }

  if (scene === 'UPCOMING') {
    return (
      <div style={screenStyle}>
        <div style={cardStyle}>
          <h1 style={{ margin: 0, marginBottom: 32, fontSize: 32, letterSpacing: '0.12em', textTransform: 'uppercase', color: '#8fd7ff', textAlign: 'center' as const }}>
            SLEDEĆI MEČEVI
          </h1>

          {summary?.upcoming?.length ? (
            <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'flex', flexDirection: 'column', gap: 16 }}>
              {summary.upcoming.map((m) => (
                <li key={`${m.court}-${m.players}`} style={{
                  display: 'flex', justifyContent: 'space-between', gap: 16, padding: '16px 22px',
                  borderRadius: 16, background: 'rgba(143, 215, 255, 0.12)', border: '1px solid rgba(143, 215, 255, 0.25)',
                }}>
                  <span style={{ fontWeight: 700, letterSpacing: '0.06em' }}>{m.court}</span>
                  <span style={{ fontSize: 18 }}>{m.players}</span>
                </li>
              ))}
            </ul>
          ) : (
            <div style={{ textAlign: 'center' as const, fontSize: 20 }}>Nema zakazanih mečeva.</div>
          )}
        </div>
      </div>
    )
  }

  if (!payload) {
    return (
      <div style={screenStyle}>
        <div style={{
          padding: '24px 32px', borderRadius: 18, background: 'rgba(8, 24, 40, 0.9)',
          border: '1px solid rgba(143, 215, 255, 0.2)', fontSize: 20, letterSpacing: '0.08em',
        }}>
          Nema aktivnog meča za teren {courtId}.
        </div>
      </div>
    )
  }

  const scoreboardCardStyle: CSSProperties = {
    width: 'min(900px, 90vw)',
    padding: '40px 48px',
    borderRadius: 28,
    background: 'rgba(5, 18, 34, 0.92)',
    boxShadow: '0 30px 80px rgba(0,0,0,0.55)',
    border: '1px solid rgba(143, 215, 255, 0.25)',
    display: 'flex',
    flexDirection: 'column',
    gap: 28,
  }

  const headerCellStyle: CSSProperties = {
    padding: '14px 18px',
    borderBottom: '1px solid rgba(255,255,255,0.14)',
    fontSize: 14,
    letterSpacing: '0.18em',
    textTransform: 'uppercase',
    textAlign: 'center' as const,
    color: 'rgba(143, 215, 255, 0.8)',
  }

  const nameCellStyle: CSSProperties = {
    padding: '20px 22px',
    borderBottom: '1px solid rgba(255,255,255,0.14)',
    fontSize: 30,
    fontWeight: 700,
    letterSpacing: '0.1em',
    textAlign: 'left' as const,
    textTransform: 'uppercase',
  }

  const scoreCellStyle: CSSProperties = {
    padding: '20px 0',
    borderBottom: '1px solid rgba(255,255,255,0.14)',
    fontSize: 30,
    fontWeight: 600,
    letterSpacing: '0.04em',
    textAlign: 'center' as const,
  }

  return (
    <div style={screenStyle}>
      <div style={scoreboardCardStyle}>
        <div style={{
          textAlign: 'center' as const, fontSize: 16, letterSpacing: '0.22em',
          color: scoreDetails?.isFinal ? '#ffd166' : '#8fd7ff', textTransform: 'uppercase',
        }}>
          {scoreDetails?.isFinal ? 'KONAČAN REZULTAT' : 'SIMULACIJA MEČA'}
        </div>

        <table style={{ width: '100%', borderCollapse: 'collapse', background: 'rgba(3, 14, 28, 0.75)', borderRadius: 22, overflow: 'hidden' }}>
          <thead>
            <tr>
              <th style={headerCellStyle}></th>
              {(scoreDetails?.sets ?? []).map((set) => (
                <th key={set.id} style={headerCellStyle}>SET {set.index + 1}</th>
              ))}
              {scoreDetails?.currentGame && <th style={headerCellStyle}>GEM</th>}
            </tr>
          </thead>
          <tbody>
            <tr>
              <td style={nameCellStyle}>{payload.PlayerA.toUpperCase()}</td>
              {(scoreDetails?.sets ?? []).map((set) => (
                <td key={`a-${set.id}`} style={scoreCellStyle}>{set.playerA}</td>
              ))}
              {scoreDetails?.currentGame && <td style={scoreCellStyle}>{scoreDetails.currentGame.playerA}</td>}
            </tr>
            <tr>
              <td style={{ ...nameCellStyle, borderBottom: 'none' }}>{payload.PlayerB.toUpperCase()}</td>
              {(scoreDetails?.sets ?? []).map((set) => (
                <td key={`b-${set.id}`} style={{ ...scoreCellStyle, borderBottom: 'none' }}>{set.playerB}</td>
              ))}
              {scoreDetails?.currentGame && <td style={{ ...scoreCellStyle, borderBottom: 'none' }}>{scoreDetails.currentGame.playerB}</td>}
            </tr>
          </tbody>
        </table>

        {(scoreDetails?.isFinal || scoreDetails?.currentGame) && (
          <div style={{
            textAlign: 'center' as const, fontSize: 18, letterSpacing: '0.1em',
            color: scoreDetails?.isFinal ? '#ffd166' : '#f2f5f8', textTransform: 'uppercase',
          }}>
            {scoreDetails?.isFinal ? (
              <>MEČ JE ZAVRŠEN — <strong>{scoreDetails.finalLine}</strong></>
            ) : (
              <>TRENUTNI GEM — <strong>{scoreDetails?.currentGame?.display}</strong></>
            )}
          </div>
        )}

        <div style={{ textAlign: 'center' as const, fontSize: 14, color: 'rgba(255,255,255,0.6)', letterSpacing: '0.08em' }}>
          Ažurirano: {new Date(payload.ServerTimeUtc).toLocaleTimeString()}
        </div>
      </div>
    </div>
  )
}
