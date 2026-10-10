// The contract of the session hub, written by hand to mirror src/Server/Realtime/Contracts.cs.
// The two files must be changed together: nothing generates this one.

/** Where the hub listens. The client adds ?sessionId=…&role=… */
export const sessionHubPath = '/hubs/session'

/** What a client is in a session (D-39: one mode, three windows). */
export type ClientRole = 'Presenter' | 'Display' | 'Participant'

export type SessionStatus = 'Lobby' | 'Live' | 'Ended'

/** The whole state, sent on connecting and on every reconnection. */
export type SessionSnapshot = {
  sessionId: string
  status: SessionStatus
  title: string
  slideIndex: number
  stepIndex: number
  /** Steps of each slide, in deck order. */
  stepCounts: number[]
  /** The join code. Null for participants, and for everyone but the teacher once the session ended. */
  code: string | null
  /** Null for participants. */
  attendance: AttendanceUpdate | null
}

export type AttendanceUpdate = {
  /** Participants with at least one open connection. */
  connectedCount: number
  /** Everyone who joined and was not removed, in the order they joined. */
  attendees: Attendee[]
}

export type Attendee = {
  attendanceId: string
  displayName: string
  connected: boolean
}

/** Messages the server sends. The names are the ones in ISessionClient. */
export const serverMessages = {
  sessionSnapshot: 'SessionSnapshot',
  sessionStarted: 'SessionStarted',
  slideChanged: 'SlideChanged',
  sessionEnded: 'SessionEnded',
  attendanceChanged: 'AttendanceChanged',
  removedFromSession: 'RemovedFromSession',
} as const

/** Methods the presenter calls. The names are the public methods of SessionHub. */
export const hubMethods = {
  startSession: 'StartSession',
  goTo: 'GoTo',
  endSession: 'EndSession',
  removeParticipant: 'RemoveParticipant',
} as const

/**
 * Why the server refused a connection: the text of the error it closes the connection with
 * contains one of these (HubErrors on the server).
 */
export type Refusal = 'NotAllowed' | 'Removed'
