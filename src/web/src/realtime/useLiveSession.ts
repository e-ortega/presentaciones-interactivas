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

/**
 * Wait before each attempt to connect, by how many have failed in a row. It never gives up:
 * a class can outlast a long outage. The count starts over when the server sends a snapshot.
 */
const retryDelays = [0, 1000, 2000, 5000]
const longestRetryDelay = 10000
const retryDelay = (failures: number) => retryDelays[failures] ?? longestRetryDelay

/**
 * Keeps this window in a live session. The server is the source of truth: the hook only
 * holds what the server last sent, and components never touch SignalR themselves.
 */
export function useLiveSession(sessionId: string, role: ClientRole): LiveSession {
  const [connection, setConnection] = useState<ConnectionStatus>('connecting')
  const [refusal, setRefusal] = useState<Refusal | null>(null)
  const [session, setSession] = useState<SessionSnapshot | null>(null)
  // Where the presenter asked to go while the server has not answered yet (see goTo).
  const [requested, setRequested] = useState<Position | null>(null)
  const requestsInFlight = useRef(0)
  // The open connection, for the actions below. A ref, not state: changing it must not re-render.
  const hub = useRef<HubConnection | null>(null)

  useEffect(() => {
    // Set when this effect is cleaned up, or when the server refuses this browser:
    // whatever is still in flight must do nothing more, and nothing reconnects.
    let finished = false
    // Attempts that failed since the server last sent a snapshot.
    let failures = 0

    function refuse(reason: Refusal) {
      finished = true
      setRefusal(reason)
      setConnection('refused')
    }

    const hubConnection = new HubConnectionBuilder()
      .withUrl(`${sessionHubPath}?sessionId=${encodeURIComponent(sessionId)}&role=${role}`)
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retry) => {
          // A 401 or 403 while reconnecting (the cookie expired) will not get better by retrying.
          const reason = refusalIn(retry.retryReason)
          if (reason !== null) {
            refuse(reason)
            return null
          }
          return retryDelay(retry.previousRetryCount)
        },
      })
      .build()
    hub.current = hubConnection

    hubConnection.on(serverMessages.sessionSnapshot, (snapshot: SessionSnapshot) => {
      failures = 0
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
      refuse('Removed')
      void hubConnection.stop()
    })

    hubConnection.onreconnecting(() => setConnection('reconnecting'))
    // After reconnecting the server sends a new snapshot, which marks the connection as up again.
    hubConnection.onclose((error) => {
      if (finished) {
        return
      }
      const reason = refusalIn(error)
      if (reason !== null) {
        refuse(reason)
        return
      }
      // Closed without a refusal: for example the server failed right after connecting.
      // Start over, counting it as a failure so a server in trouble is not hammered.
      failures++
      setConnection('reconnecting')
      void start()
    })

    async function start() {
      // Neither the first connection nor one closed by the server is covered by automatic
      // reconnect, so both are retried here.
      for (;;) {
        await delay(retryDelay(failures))
        if (finished) {
          return
        }
        try {
          await hubConnection.start()
          return
        } catch (error) {
          if (finished || hubConnection.state !== HubConnectionState.Disconnected) {
            return
          }
          const reason = refusalIn(error instanceof Error ? error : undefined)
          if (reason !== null) {
            refuse(reason) // retrying would get the same answer
            return
          }
          failures++
        }
      }
    }

    void start()

    return () => {
      finished = true
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

  /**
   * Moves the session. The new position is shown at once, so the presenter does not wait for
   * the round trip on every slide. While any request is on its way the screen shows the last
   * position asked for, whatever the server echoes in the meantime; when all have been
   * answered it goes back to showing what the server says, which by then is where the last
   * accepted request left it. So quick presses are not undone by late answers, and a refused
   * request simply falls back to the server's position.
   */
  async function goTo(position: Position): Promise<void> {
    requestsInFlight.current++
    setRequested(position)
    try {
      await invoke(hubMethods.goTo, position.slideIndex, position.stepIndex)
    } finally {
      requestsInFlight.current--
      if (requestsInFlight.current === 0) {
        setRequested(null)
      }
    }
  }

  return {
    connection,
    refusal,
    session: session && requested ? { ...session, ...requested } : session,
    startSession: () => invoke(hubMethods.startSession),
    goTo,
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
