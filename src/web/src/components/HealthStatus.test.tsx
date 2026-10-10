import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import { expect, test, vi } from 'vitest'
import { HealthStatus } from './HealthStatus'

function renderHealthStatus() {
  // A fresh client per test, without retries, so a failure shows up at once.
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={queryClient}>
      <HealthStatus />
    </QueryClientProvider>,
  )
}

test('says the server responds when the health check succeeds', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('Healthy')))

  renderHealthStatus()

  expect(screen.getByRole('status')).toHaveTextContent('Comprobando la conexión con el servidor…')
  expect(await screen.findByText('El servidor responde.')).toBeInTheDocument()
})

test('says there is no connection when the health check fails', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('', { status: 503 })))

  renderHealthStatus()

  expect(await screen.findByText('No hay conexión con el servidor.')).toBeInTheDocument()
})
