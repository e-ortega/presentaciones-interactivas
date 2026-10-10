import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { expect, test, vi } from 'vitest'
import App from './App'
import { hub } from './test/fakeHub'
import { jsonResponse, renderWithQuery, stubFetch } from './test/render'

vi.mock('@microsoft/signalr', async () => (await import('./test/fakeHub')).fakeSignalR)

const access = new Response(null, { status: 204 })
const healthy = new Response('Healthy')

const deck = {
  id: 'deck-1',
  title: 'Semana 01',
  status: 'Ready',
  failure: null,
  slideCount: 2,
  createdAt: '2026-10-10T05:00:00+00:00',
}

test('the home address is the front door for students, with a way through for the teacher', async () => {
  stubFetch({
    'GET /api/admin/access': access,
    'GET /health': healthy,
    'GET /api/admin/presentations': jsonResponse([]),
  })
  const user = userEvent.setup()

  renderWithQuery(<App />, '/')
  expect(await screen.findByRole('heading', { name: 'Unirme a una clase' })).toBeInTheDocument()

  await user.click(screen.getByRole('link', { name: 'Soy profesor o profesora' }))
  expect(await screen.findByRole('heading', { name: 'Mis presentaciones' })).toBeInTheDocument()
})

test('"Ensayar" opens the presenter view of that presentation, with no students', async () => {
  stubFetch({
    'GET /api/admin/access': access,
    'GET /health': healthy,
    'GET /api/admin/presentations': jsonResponse([deck]),
    'GET /api/admin/presentations/deck-1': jsonResponse({
      ...deck,
      slides: [
        { index: 0, speakerNotes: 'Primera nota', stepCount: 1 },
        { index: 1, speakerNotes: 'Segunda nota', stepCount: 1 },
      ],
    }),
  })
  const user = userEvent.setup()

  renderWithQuery(<App />, '/admin')
  await user.click(await screen.findByRole('link', { name: 'Ensayar Semana 01' }))

  expect(await screen.findByRole('img', { name: 'Diapositiva 1 de 2' })).toBeInTheDocument()
  expect(screen.getByRole('complementary', { name: 'Guion' })).toHaveTextContent('Primera nota')
})

test('"Iniciar clase" opens a session and goes to its lobby', async () => {
  const session = { id: 'session-1', code: '12345678', status: 'Lobby', presentationId: 'deck-1', title: 'Semana 01' }
  const fetchMock = stubFetch({
    'GET /api/admin/access': access,
    'GET /health': healthy,
    'GET /api/admin/presentations': jsonResponse([deck]),
    'POST /api/admin/sessions': jsonResponse(session, 201),
    'GET /api/admin/sessions/session-1': jsonResponse({ ...session, slideIndex: 0, stepIndex: 0, slides: [] }),
  })
  const user = userEvent.setup()

  renderWithQuery(<App />, '/admin')
  await user.click(await screen.findByRole('button', { name: 'Iniciar clase con Semana 01' }))

  expect(await screen.findByText('Conectando con la clase…')).toBeInTheDocument()
  expect(fetchMock).toHaveBeenCalledWith(
    '/api/admin/sessions',
    expect.objectContaining({ method: 'POST', body: JSON.stringify({ presentationId: 'deck-1' }) }),
  )
  expect(hub().url).toBe('/hubs/session?sessionId=session-1&role=Presenter')
})

test('the live session of a class asks for the access key too', async () => {
  stubFetch({ 'GET /api/admin/access': new Response(null, { status: 401 }) })

  renderWithQuery(<App />, '/present/session-1')

  expect(await screen.findByLabelText('Clave de acceso')).toBeInTheDocument()
})

test('the presenter view asks for the access key too', async () => {
  stubFetch({ 'GET /api/admin/access': new Response(null, { status: 401 }) })

  renderWithQuery(<App />, '/admin/presentations/deck-1/present')

  expect(await screen.findByLabelText('Clave de acceso')).toBeInTheDocument()
})

test('an unknown address shows a way back instead of a blank page', async () => {
  renderWithQuery(<App />, '/no-such-page')

  expect(await screen.findByText('Esta página no existe.')).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'Ir a mis presentaciones' })).toHaveAttribute('href', '/admin')
})
