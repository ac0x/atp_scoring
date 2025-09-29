import { useEffect } from 'react'
import { useSignalR } from '@/hooks/useSignalR'
import { useScoreStore } from '@/stores/useScoreStore'
import { fetchCourts, fetchMatches } from '@/api'

export default function ControlPanel() {
  useSignalR()
  const { courts, matches, setCourts, setMatches, connectionStatus, lastPacketAt } = useScoreStore()

  useEffect(()=>{ (async()=>{
    try{ const [c,m]=await Promise.all([fetchCourts(), fetchMatches()]); setCourts(c); setMatches(m) }
    catch(e){ console.error(e) }
  })() }, [setCourts, setMatches])

  const openOverlay = (courtId: string) => {
    const url = `${location.origin}/overlay/court/${courtId}?autoFs=1`
    window.open(url, '_blank', 'popup=yes,noopener,noreferrer,resizable=yes,scrollbars=no')
  }

  return (
    <div style={{padding:16}}>
      <h1>Control Panel (DEMO)</h1>
      <div style={{ margin:'8px 0' }}>
        <b>Connection:</b> {connectionStatus}
        <span style={{ color:'#666' }}>
          {lastPacketAt ? ` | last packet ${new Date(lastPacketAt).toLocaleTimeString()}` : ''}
        </span>
      </div>

      <div style={{display:'grid',gridTemplateColumns:'1fr 1fr',gap:16}}>
        <section>
          <h3>Courts</h3>
          <ul>
            {courts.map(c=>(
              <li key={c.courtId} style={{display:'flex',gap:8,alignItems:'center'}}>
                <span>[{c.courtId}] {c.name}</span>
                <button onClick={()=>openOverlay(c.courtId)}>Open overlay</button>
              </li>
            ))}
          </ul>
        </section>

        <section>
          <h3>Matches</h3>
          <ul>
            {matches.map(m=>(
              <li key={m.matchId}>[{m.courtId}] {m.playerA} vs {m.playerB}</li>
            ))}
          </ul>
        </section>
      </div>
    </div>
  )
}
