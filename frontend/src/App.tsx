import { useEffect, useMemo, useState } from 'react'
import './App.css'

type MatchScore = {
  players: string
  score: string[]
  court?: string
}

type PointEvent = {
  winner: string
  description: string
}

type Phase = 'live' | 'ads' | 'summary'

const INITIAL_COMPLETED: MatchScore[] = [
  { players: 'Hamad Medjedovic vs Jannik Sinner', score: ['6:2', '6:4'] },
  { players: 'Daniil Medvedev vs Alexander Zverev', score: ['5:7', '2:6'] },
]

const CURRENT_MATCH_FINAL_SCORE = ['6:4', '7:5']

function App() {
  const [phase, setPhase] = useState<Phase>('live')
  const [playedEvents, setPlayedEvents] = useState<PointEvent[]>([])
  const [currentMatchFinished, setCurrentMatchFinished] = useState(false)
  const [completedMatches, setCompletedMatches] = useState<MatchScore[]>(INITIAL_COMPLETED)
  const [adsCountdown, setAdsCountdown] = useState(10)

  const pointSequence: PointEvent[] = useMemo(
    () => [
      {
        winner: 'Novak Djokovic',
        description: 'servis kojim iznuđuje grešku Alcaraza',
      },
      {
        winner: 'Novak Djokovic',
        description: 'forhend winner po paraleli za pobedu',
      },
    ],
    []
  )

  const remainingPoints = pointSequence.length - playedEvents.length

  useEffect(() => {
    if (phase !== 'live') return

    if (playedEvents.length < pointSequence.length) {
      const timer = setTimeout(() => {
        setPlayedEvents((prev) => [...prev, pointSequence[prev.length]])
      }, 2000)
      return () => clearTimeout(timer)
    }

    if (playedEvents.length === pointSequence.length && !currentMatchFinished) {
      setCurrentMatchFinished(true)
    }
  }, [phase, playedEvents, pointSequence, currentMatchFinished])

  useEffect(() => {
    if (phase === 'live' && currentMatchFinished) {
      setCompletedMatches((prev) => [
        ...prev,
        {
          players: 'Novak Djokovic vs Carlos Alcaraz',
          score: CURRENT_MATCH_FINAL_SCORE,
        },
      ])
      setPhase('ads')
    }
  }, [phase, currentMatchFinished])

  useEffect(() => {
    if (phase === 'ads') setAdsCountdown(10)
  }, [phase])

  useEffect(() => {
    if (phase !== 'ads') return

    if (adsCountdown > 0) {
      const timer = setTimeout(() => setAdsCountdown((value) => value - 1), 1000)
      return () => clearTimeout(timer)
    }

    if (adsCountdown === 0) {
      setPhase('summary')
    }
  }, [phase, adsCountdown])

  const upcomingMatches = useMemo(
    () => [
      { court: 'CENTER COURT', players: 'Roger Federer vs Rafael Nadal' },
      { court: 'COURT 2', players: 'Andy Murray vs Stan Wawrinka' },
    ],
    []
  )

  return (
    <div className="app">
      <header className="header">
        <h1>ATP Scoring Simulator</h1>
      </header>

      <main className="content">
        {phase === 'live' && (
          <section className="panel live-panel">
            <h2>Trenutni meč</h2>
            <div className="match-card">
              <div className="match-header">
                <span className="players">Novak Djokovic vs Carlos Alcaraz</span>
                <span className="court">CENTER COURT</span>
              </div>
              <div className="score-line">
                <span>Rezultat setova:</span>
                <span className="score-values">6:4, 5:4*</span>
              </div>
              <div className="points-remaining">
                Preostalo poena do kraja meča: <strong>{remainingPoints}</strong>
              </div>
              <div className="events">
                <h3>Simulacija poena</h3>
                <ul>
                  {playedEvents.map((event, index) => (
                    <li key={`${event.winner}-${index}`}>
                      <span className="event-index">Poen {index + 1}:</span>{' '}
                      <span className="event-description">
                        {event.winner} - {event.description}
                      </span>
                    </li>
                  ))}
                </ul>
                {remainingPoints === 0 && (
                  <div className="match-complete">Meč je završen! Rezultat: 6:4, 7:5</div>
                )}
              </div>
            </div>
          </section>
        )}

        {phase === 'summary' && (
          <section className="panel summary-panel">
            <h2>GOTOVI MEČEVI:</h2>
            <ul className="finished-list">
              {completedMatches.map((match, index) => (
                <li key={`${match.players}-${index}`} className="finished-item">
                  <div className="finished-players">{match.players}</div>
                  <div className="finished-score">{match.score.join(' ')}</div>
                </li>
              ))}
            </ul>

            <div className="upcoming">
              <h3>SLEDEĆI MEČEVI:</h3>
              <ul>
                {upcomingMatches.map((match) => (
                  <li key={match.players} className="upcoming-item">
                    <span className="upcoming-court">{match.court}</span>
                    <span className="upcoming-players">{match.players}</span>
                  </li>
                ))}
              </ul>
            </div>
          </section>
        )}
      </main>

      {phase === 'ads' && (
        <div className="ads-overlay">
          <div className="ads-content">
            <span className="ads-label">REKLAME</span>
            <span className="ads-countdown">Nastavak za {adsCountdown} s</span>
          </div>
        </div>
      )}
    </div>
  )
}

export default App
