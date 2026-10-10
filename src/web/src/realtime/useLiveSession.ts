import { HubConnectionBuilder, HubConnectionState, type HubConnection } from '@microsoft/signalr'
import { useEffect, useRef, useState } from 'react'
import type { Position } from '../presenter/navigation'
import {
  hubMethods,
  serverMessages,
  sessionHubPath,
  type AttendanceUpdate,
  type ClientRole,
  type Refusal,
  type SessionSnapshot,
} from './contracts'

/** How the link to the server is doing. */
export type ConnectionStatus =
  /** First connection, not established yet. */
  | 'connecting'
  | 'connected'
  /** The connection dropped and is being re-established. The last known state stays on screen. */
  | 'reconnecting'
  /** The server will not let this browser in: see `refusal`. No more attempts are made. */
  | 'refused'

export type LiveSession = {
  connection: ConnectionStatus
  refusal: Refusal | null
  /** What the server last said about the session. Null until the first snapshot arrives. */
  session: SessionSnapshot | null
  /** Presenter only. Each one resolves when the server has done it, and rejects if it refuses. */
  startSession: () => Promise<void>
  goTo: (position: Position) => Promise<void>
  endSession: () => Promise<void>
  removeParticipant: (attendanceId: string) => Promise<void>
}

/** Delay before each reconnection attempt. It never gives up: a class can outlast a long outage. */
const retryDelays = [0, 1000, 2000, 5000]
const longestRetryDelay = 10000

/**
 * Keeps this window in a live session. The server is the source of truth: the hook only
 * holds what the server last sent, and components never touch SignalR themselves.
 */
export function useLiveSession(sessionId: string, role: ClientRole): LiveSession {
  const [connection, setConnection] = useState<ConnectionStatus>('connecting')
  const [refusal, setRefusal] = useState<Refusal | null>(null)
  const [session, setSession] = useState<SessionSnapshot | null>(null)
  // The open connection, for the actions below. A ref, not state: changing it must not re-render.
  const hub = useRef<HubConnection | null>(null)

  useEffect(() => {
    const hubConnection = new HubConnectionBuilder()
      .withUrl(`${sessionHubPath}?sessionId=${encodeURIComponent(sessionId)}&role=${role}`)
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retry) => retryDelays[retry.previousRetryCount] ?? longestRetryDelay,
      })
      .build()
    hub.current = hubConnection
    // Set when this effect is cleaned up: whatever is still in flight must do nothing more.
    let abandoned = false

    hubConnection.on(serverMessages.sessionSnapshot, (snapshot: SessionSnapshot) => {
      setSession(snapshot)
      setConnection('connected')
    })
    hubConnection.on(serverMessages.sessionStarted, (slideIndex: number, stepIndex: number) =>
      setSession((current) => current && { ...current, status: 'Live', slideIndex, stepIndex }),
    )
    hubConnection.on(serverMessages.slideChanged, (slideIndex: number, stepIndex: number) =>
      setSession((current) => current && { ...current, slideIndex, stepIndex }),
    )
    hubConnection.on(serverMessages.sessionEnded, () =>
      setSession((current) => current && { ...current, status: 'Ended' }),
    )
    hubConnection.on(serverMessages.attendanceChanged, (attendance: AttendanceUpdate) =>
      setSession((current) => current && { ...current, attendance }),
    )
    hubConnection.on(serverMessages.removedFromSession, () => {
      setRefusal('Removed')
      setConnection('refused')
      void hubConnection.stop()
    })

    hubConnection.onreconnecting(() => setConnection('reconnecting'))
    // After reconnecting the server sends a new snapshot, which marks the connection as up again.
    hubConnection.onclose((error) => {
      if (abandoned) {
        return
      }
      const reason = refusalIn(error)
      if (reason !== null) {
        setRefusal(reason)
        setConnection('refused')
        return
      }
      // Closed for good without a refusal (the retries only stop if the browser is told to): start over.
      setConnection('reconnecting')
      void start()
    })

    async function start() {
      // The very first connection is not covered by automatic reconnect, so it is retried here.
      for (let attempt = 0; ; attempt++) {
        if (abandoned) {
          return
        }
        try {
          await hubConnection.start()
          return
        } catch (error) {
          if (abandoned || hubConnection.state !== HubConnectionState.Disconnected) {
            return
          }
          const reason = refusalIn(error instanceof Error ? error : undefined)
          if (reason !== null) {
            setRefusal(reason)
            setConnection('refused')
            return // retrying would get the same answer
          }
          await delay(retryDelays[attempt] ?? longestRetryDelay)
        }
      }
    }

    void start()

    return () => {
      abandoned = true
      hub.current = null
      void hubConnection.stop()
    }
  }, [sessionId, role])

  function invoke(method: string, ...args: unknown[]): Promise<void> {
    if (hub.current === null) {
      return Promise.reject(new Error('Not connected to the session.'))
    }
    return hub.current.invoke(method, ...args)
  }

  return {
    connection,
    refusal,
    session,
    startSession: () => invoke(hubMethods.startSession),
    goTo: async (position) => {
      // Shown at once, so the presenter does not wait for the round trip on every slide.
      // The server's SlideChanged then confirms it; if the server refuses, go back.
      const before = session && { slideIndex: session.slideIndex, stepIndex: session.stepIndex }
      setSession((current) => current && { ...current, ...position })
      try {
        await invoke(hubMethods.goTo, position.slideIndex, position.stepIndex)
      } catch (error) {
        setSession((current) => (current && before ? { ...current, ...before } : current))
        throw error
      }
    },
    endSession: () => invoke(hubMethods.endSession),
    removeParticipant: (attendanceId) => invoke(hubMethods.removeParticipant, attendanceId),
  }
}

/**
 * Why the server will not let this browser in, read from the error of a closed or failed
 * connection: the reason the hub gives, or a 401/403 to a browser without the right cookie.
 * Null for anything else, such as a network failure, which is worth retrying.
 */
function refusalIn(error: Error | undefined): Refusal | null {
  const message = error?.message ?? ''
  if (message.includes('Removed')) {
    return 'Removed'
  }
  if (message.includes('NotAllowed') || /status code '?(401|403)/i.test(message)) {
    return 'NotAllowed'
  }
  return null
}

function delay(milliseconds: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, milliseconds))
}
