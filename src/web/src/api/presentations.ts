import { ensureOk } from './http'
import type { PresentationDetail, PresentationSummary } from './types'

export async function listPresentations(): Promise<PresentationSummary[]> {
  const response = await ensureOk(await fetch('/api/admin/presentations'))
  return response.json()
}

/** Uploads a PPTX. The import runs in the background: the result starts as "Pending". */
export async function uploadPresentation(file: File): Promise<PresentationSummary> {
  const form = new FormData()
  form.append('file', file)
  const response = await ensureOk(await fetch('/api/admin/presentations', { method: 'POST', body: form }))
  return response.json()
}

/** One presentation with its slides and speaker notes. Throws an ApiError (404) when it does not exist. */
export async function getPresentation(presentationId: string): Promise<PresentationDetail> {
  const response = await ensureOk(await fetch(`/api/admin/presentations/${encodeURIComponent(presentationId)}`))
  return response.json()
}

/** The image of one step of one slide. Navigation is always by slide and step (D-26). */
export function stepImageUrl(presentationId: string, position: { slideIndex: number; stepIndex: number }): string {
  return `/api/admin/presentations/${presentationId}/slides/${position.slideIndex}/steps/${position.stepIndex}`
}
