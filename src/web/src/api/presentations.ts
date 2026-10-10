import { ensureOk } from './http'
import type { PresentationSummary } from './types'

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

export function firstSlideImageUrl(presentationId: string): string {
  return `/api/admin/presentations/${presentationId}/slides/0/steps/0`
}
