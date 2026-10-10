import { ensureOk } from './http'
import type { SessionDetail, SessionInfo } from './types'

/** Opens a session of a presentation: its lobby, with a new join code. */
export async function createSession(presentationId: string): Promise<SessionInfo> {
  const response = await fetch('/api/admin/sessions', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ presentationId }),
  })
  return (await ensureOk(response)).json()
}

/**
 * A session with its slides and speaker notes. The notes come this way, over HTTP with the
 * teacher cookie, and never through the hub. Throws an ApiError (404) when it does not exist.
 */
export async function getSession(sessionId: string): Promise<SessionDetail> {
  const response = await ensureOk(await fetch(`/api/admin/sessions/${encodeURIComponent(sessionId)}`))
  return response.json()
}

/** The join code the way people read it aloud and type it: 8888-6666. */
export function formatSessionCode(code: string): string {
  return `${code.slice(0, 4)}-${code.slice(4)}`
}

/** The address a student opens to join, which is also what the QR code holds. */
export function joinUrl(code: string): string {
  return `${window.location.origin}/s/${code}`
}
