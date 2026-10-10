import { ensureOk } from './http'
import type { components } from './schema'

export type JoinInfo = components['schemas']['JoinInfo']

export type JoinForm = {
  name: string
  email: string
  /** True only if the student ticked the consent box. */
  consent: boolean
}

/** The code as the server stores it, from what someone typed: digits only. */
export function normalizeSessionCode(typed: string): string {
  return typed.replace(/[\s-]/g, '')
}

/** Whether what was typed can be a session code: 8 digits, with or without a dash or spaces. */
export function looksLikeSessionCode(typed: string): boolean {
  return /^\d{8}$/.test(normalizeSessionCode(typed))
}

/** Which class a code leads to. Throws an ApiError (404) when no open class has that code. */
export async function getJoinInfo(code: string): Promise<JoinInfo> {
  const response = await ensureOk(await fetch(`/api/join/${encodeURIComponent(code)}`))
  return response.json()
}

/**
 * Joins the class. The server answers with a cookie that identifies this browser in the
 * session from then on. Throws an ApiError whose `code` says what was wrong.
 */
export async function joinSession(code: string, form: JoinForm): Promise<{ sessionId: string }> {
  const response = await fetch(`/api/join/${encodeURIComponent(code)}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(form),
  })
  return (await ensureOk(response)).json()
}

/** The image of a step, for whoever is in the session. A student only gets the one on screen. */
export function sessionStepImageUrl(sessionId: string, position: { slideIndex: number; stepIndex: number }): string {
  return `/api/sessions/${sessionId}/slides/${position.slideIndex}/steps/${position.stepIndex}`
}

/** Presenter: a one-time link that opens the window to share on another computer. */
export async function createDisplayLink(sessionId: string): Promise<string> {
  const response = await ensureOk(await fetch(`/api/admin/sessions/${sessionId}/display-token`, { method: 'POST' }))
  const body: components['schemas']['DisplayToken'] = await response.json()
  return `${window.location.origin}/display/${sessionId}#token=${body.token}`
}

/** Display window: trades the one-time token of its link for the cookie that lets it in. */
export async function redeemDisplayToken(token: string): Promise<void> {
  const response = await fetch('/api/display/access', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ token }),
  })
  await ensureOk(response)
}
