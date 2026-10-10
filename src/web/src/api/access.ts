import { ensureOk } from './http'

/** Whether this browser already holds the teacher cookie. */
export async function hasAccess(): Promise<boolean> {
  const response = await fetch('/api/admin/access')
  if (response.status === 401) {
    return false
  }
  await ensureOk(response)
  return true
}

/** Exchanges the access key for the teacher cookie. Throws an ApiError (401) when the key is wrong. */
export async function enterAccessKey(key: string): Promise<void> {
  const response = await fetch('/api/access', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ key }),
  })
  await ensureOk(response)
}
