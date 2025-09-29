import { HubConnectionBuilder, LogLevel, HubConnection } from '@microsoft/signalr'
import { useEffect, useRef } from 'react'
import { useScoreStore } from '@/stores/useScoreStore'
import type { ScorePayload } from '@/types'

export function useSignalR() {
  const url = import.meta.env.VITE_HUB_URL as string
  const connRef = useRef<HubConnection|null>(null)
  const setStatus = useScoreStore(s=>s.setStatus)
  const setError  = useScoreStore(s=>s.setError)
  const upsert    = useScoreStore(s=>s.upsert)
  const setScene  = useScoreStore(s => s.setScene)

  useEffect(()=>{
    let mounted = true
    const conn = new HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect([0,2000,5000,10000,30000])
      .configureLogging(LogLevel.Information)
      .build()
    connRef.current = conn

    conn.on('ScoreUpdate', (p: ScorePayload)=> upsert(p))
    conn.onreconnecting(e=>{ setStatus('reconnecting'); setError(e?.message) })
    conn.onreconnected(()=>{ setStatus('connected'); setError(undefined) })
    conn.onclose(e=>{ setStatus('disconnected'); setError(e?.message) })
    const onScene = (p: { CourtId: string; Scene: string; ServerTimeUtc: string }) =>
      setScene(p.CourtId, p.Scene)
    conn.on('SceneSwitch', onScene)   // ← NOVO
    conn.on('sceneswitch', onScene) 

    ;(async()=>{ try{
      setStatus('connecting'); await conn.start(); if(!mounted) return; setStatus('connected')
    }catch(e:any){ if(!mounted) return; setStatus('error'); setError(e?.message??String(e)) } })()

    return ()=>{ mounted=false; conn.stop().catch(()=>{}); connRef.current=null }
  }, [url, setStatus, setError, upsert, setScene])

  return {
    invoke: async (method:string, ...args:any[])=>{
      if(!connRef.current) throw new Error('Connection not ready')
      return connRef.current.invoke(method, ...args)
    }
  }
}
