import { QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import type { ReactElement } from 'react'
import { vi } from 'vitest'
import { createQueryClient } from '../api/queryClient'

/** Renders a component that uses TanStack Query, with the app's client, fresh and without retries. */
export function renderWithQuery(ui: ReactElement) {
  const queryClient = createQueryClient({ queries: { retry: false }, mutations: { retry: false } })
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>)
}

/** A JSON response, as the server would send it. */
export function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

/**
 * Replaces fetch for one test. `routes` maps "METHOD /path" to the response to give;
 * a function is called on every request, so it can answer differently each time.
 */
export function stubFetch(routes: Record<string, Response | (() => Response)>) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = typeof input === 'string' ? input : input instanceof URL ? input.href : input.url
    const key = `${init?.method ?? 'GET'} ${url}`
    const route = routes[key]
    if (route === undefined) {
      throw new Error(`Unexpected request in test: ${key}`)
    }
    // A Response body can be read only once, so every call gets its own copy.
    return typeof route === 'function' ? route() : route.clone()
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}
