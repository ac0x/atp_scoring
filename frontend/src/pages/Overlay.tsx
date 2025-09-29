import { useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useSignalR } from '@/hooks/useSignalR'
import { useScoreStore } from '@/stores/useScoreStore'

export default function Overlay() {
  useSignalR()
  const { courtId } = useParams()
  const payload = useScoreStore(s => courtId ? s.byCourt[courtId] : undefined)
  const scene   = useScoreStore(s => (courtId ? s.sceneByCourt[courtId] : undefined)) // ← NOVO

  useEffect(()=>{
    const p = new URLSearchParams(location.search)
    if(p.get('autoFs')==='1'){
      setTimeout(async ()=>{
        try{ await document.documentElement.requestFullscreen() }catch{}
      }, 50)
    }
  },[])

  // Ako je scena ADS, prikaži “REKLAME”
  if (scene === 'ADS') {
    return (
      <div style={{
        width:'100vw',height:'100vh',display:'flex',alignItems:'center',justifyContent:'center',
        background:'#000',color:'#fff',fontSize:56,fontWeight:800,letterSpacing:2
      }}>
        REKLAME
      </div>
    )
  }


  return (
    <div style={{padding:16, fontFamily:'system-ui'}}>
      <div style={{ marginBottom:8 }}><Link to="/">{'← back'}</Link></div>
      <h2>Overlay (Court {courtId})</h2>
      {!payload ? (
        <div>No live data yet for court {courtId}</div>
      ) : (
        <div style={{ fontSize:28 }}>
          <div><b>{payload.PlayerA}</b> vs <b>{payload.PlayerB}</b></div>
          <div style={{ marginTop:8 }}>Score: {payload.Score}</div>
          <div style={{ marginTop:4, fontSize:12, color:'#666' }}>
            Court: {payload.CourtId} | Server: {new Date(payload.ServerTimeUtc).toLocaleTimeString()}
          </div>
        </div>
      )}
    </div>
  )
}
