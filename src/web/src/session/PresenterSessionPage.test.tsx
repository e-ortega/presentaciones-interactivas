import { act, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Route, Routes } from 'react-router'
import { beforeEach, expect, test, vi } from 'vitest'
import type { SessionDetail } from '../api/types'
import { AccessGate } from '../components/AccessGate'
import type { AttendanceUpdate, SessionSnapshot } from '../realtime/contracts'
import { FakeHubConnection, hub } from '../test/fakeHub'
import { jsonResponse, renderWithQuery, stubFetch } from '../test/render'
import { PresenterSessionPage } from './PresenterSessionPage'

vi.mock('@microsoft/signalr', async () => (await import('../test/fakeHub')).fakeSignalR)

const details: SessionDetail = {
  id: 'session-1',
  code: '88886666',
  status: 'Lobby',
  slideIndex: 0,
  stepIndex: 0,
  presentationId: 'deck-1',
  title: 'Semana 01',
  slides: [
    { index: 0, speakerNotes: 'Saludar a la clase.', stepCount: 1 },
    { index: 1, speakerNotes: 'Explicar qué es un ERP.', stepCount: 1 },
    { index: 2, speakerNotes: 'Cerrar.', stepCount: 1 },
  ],
}

function snapshot(overrides: Partial<SessionSnapshot> = {}): SessionSnapshot {
  return {
    sessionId: 'session-1',
    status: 'Lobby',
    title: 'Semana 01',
    slideIndex: 0,
    stepIndex: 0,
    stepCounts: [1, 1, 1],
    code: '88886666',
    attendance: { connectedCount: 0, attendees: [] },
    ...overrides,
  }
}

const twoStudents: AttendanceUpdate = {
  connectedCount: 1,
  attendees: [
    { attendanceId: 'a1', displayName: 'Ana Mora', connected: true },
    { attendanceId: 'a2', displayName: 'Luis Vega', connected: false },
  ],
}

beforeEach(() => {
  FakeHubConnection.current = undefined
  vi.spyOn(window, 'confirm').mockReturnValue(true)
})

/** Opens the page and waits until it has connected to the hub. */
async function openSession(response: Response = jsonResponse(details)) {
  stubFetch({ 'GET /api/admin/sessions/session-1': response })
  renderWithQuery(
    <Routes>
      <Route path="/present/:sessionId" element={<PresenterSessionPage />} />
      <Route path="/admin" element={<p>Presentation list</p>} />
    </Routes>,
    '/present/session-1',
  )
  await waitFor(() => expect(FakeHubConnection.current?.start).toHaveBeenCalled())
  return userEvent.setup()
}

test('connects to the session as the presenter and waits for the server before showing anything', async () => {
  await openSession()

  expect(hub().url).toBe('/hubs/session?sessionId=session-1&role=Presenter')
  expect(screen.getByText('Conectando con la clase…')).toBeInTheDocument()
})

test('the lobby shows how to join: the code as people say it, the address and a QR code', async () => {
  await openSession()

  hub().receive('SessionSnapshot', snapshot())

  expect(await screen.findByRole('heading', { name: 'Semana 01' })).toBeInTheDocument()
  expect(screen.getByText('8888-6666')).toBeInTheDocument()
  expect(screen.getByText(`${window.location.origin}/s/88886666`)).toBeInTheDocument()
  expect(screen.getByTitle('Código QR para unirse a la clase')).toBeInTheDocument()
  expect(screen.getByText(/Todavía no ha entrado nadie/)).toBeInTheDocument()
})

test('the attendance list fills in as students join, and marks who lost their connection', async () => {
  await openSession()
  hub().receive('SessionSnapshot', snapshot())

  hub().receive('AttendanceChanged', twoStudents)

  expect(await screen.findByRole('heading', { name: 'Lista de asistencia · 2 personas' })).toBeInTheDocument()
  const rows = screen.getAllByRole('listitem')
  expect(rows[0]).toHaveTextContent('Ana Mora')
  expect(rows[0]).not.toHaveTextContent('sin conexión')
  expect(rows[1]).toHaveTextContent('Luis Vega')
  expect(rows[1]).toHaveTextContent('sin conexión')
})

test('removing a student asks first, and does nothing if the teacher says no', async () => {
  const user = await openSession()
  hub().receive('SessionSnapshot', snapshot({ attendance: twoStudents }))

  vi.mocked(window.confirm).mockReturnValueOnce(false)
  await user.click(await screen.findByRole('button', { name: 'Sacar a Luis Vega' }))
  expect(hub().calls('RemoveParticipant')).toEqual([])

  await user.click(screen.getByRole('button', { name: 'Sacar a Luis Vega' }))
  expect(window.confirm).toHaveBeenLastCalledWith(expect.stringContaining('¿Sacar a Luis Vega de la clase?'))
  expect(hub().calls('RemoveParticipant')).toEqual([['a2']])
})

test('starting the class asks the server, and the presenter view opens when the server says it started', async () => {
  const user = await openSession()
  hub().receive('SessionSnapshot', snapshot())

  await user.click(await screen.findByRole('button', { name: 'Empezar la clase' }))
  expect(hub().calls('StartSession')).toEqual([[]])
  expect(screen.queryByRole('img', { name: /Diapositiva/ })).not.toBeInTheDocument() // not until the server confirms

  hub().receive('SessionStarted', 0, 0)

  expect(await screen.findByRole('img', { name: 'Diapositiva 1 de 3' })).toHaveAttribute(
    'src',
    '/api/admin/presentations/deck-1/slides/0/steps/0',
  )
  expect(screen.getByRole('complementary', { name: 'Guion' })).toHaveTextContent('Saludar a la clase.')
  expect(screen.getByText('8888-6666')).toBeInTheDocument() // still in sight for whoever arrives late
})

test('moving to another slide tells the server the slide and the step, and shows it at once', async () => {
  const user = await openSession()
  hub().receive('SessionSnapshot', snapshot({ status: 'Live' }))
  hub().holdCalls = true // the server takes its time

  await user.click(await screen.findByRole('button', { name: 'Siguiente' }))

  // Asked of the server, and already on screen without waiting for the answer.
  expect(hub().calls('GoTo')).toEqual([[1, 0]])
  expect(screen.getByRole('img', { name: 'Diapositiva 2 de 3' })).toBeInTheDocument()

  // The server moves the session, tells everyone, and then answers the call.
  hub().receive('SlideChanged', 1, 0)
  await hub().answerCalls()
  expect(screen.getByRole('img', { name: 'Diapositiva 2 de 3' })).toBeInTheDocument()

  hub().holdCalls = false
  await user.keyboard('{ArrowRight}')
  expect(hub().calls('GoTo')).toEqual([[1, 0], [2, 0]])
})

test('if the server refuses a slide change the view goes back and says so', async () => {
  const user = await openSession()
  hub().receive('SessionSnapshot', snapshot({ status: 'Live' }))
  hub().refuse = 'GoTo'

  await user.click(await screen.findByRole('button', { name: 'Siguiente' }))

  expect(await screen.findByRole('alert')).toHaveTextContent('No se pudo hacer eso.')
  expect(screen.getByRole('img', { name: 'Diapositiva 1 de 3' })).toBeInTheDocument()
})

test('the server decides the slide: a change that comes from it moves this window too', async () => {
  await openSession()
  hub().receive('SessionSnapshot', snapshot({ status: 'Live' }))

  hub().receive('SlideChanged', 2, 0) // the teacher's other window moved

  expect(await screen.findByRole('img', { name: 'Diapositiva 3 de 3' })).toBeInTheDocument()
  expect(screen.getByRole('complementary', { name: 'Guion' })).toHaveTextContent('Cerrar.')
})

test('during the class the bar shows how many students are connected', async () => {
  await openSession()
  hub().receive('SessionSnapshot', snapshot({ status: 'Live', attendance: twoStudents }))

  expect(await screen.findByText('1 conectado')).toBeInTheDocument()

  hub().receive('AttendanceChanged', { ...twoStudents, connectedCount: 2 })
  expect(await screen.findByText('2 conectados')).toBeInTheDocument()
})

test('ending the class asks first, and the view closes when the server says it ended', async () => {
  const user = await openSession()
  hub().receive('SessionSnapshot', snapshot({ status: 'Live' }))

  vi.mocked(window.confirm).mockReturnValueOnce(false)
  await user.click(await screen.findByRole('button', { name: 'Terminar la clase' }))
  expect(hub().calls('EndSession')).toEqual([])

  await user.click(screen.getByRole('button', { name: 'Terminar la clase' }))
  expect(hub().calls('EndSession')).toEqual([[]])

  hub().receive('SessionEnded')
  expect(await screen.findByText('La clase terminó.')).toBeInTheDocument()
})

test('cancelling from the lobby ends the session and goes back to the list', async () => {
  const user = await openSession()
  hub().receive('SessionSnapshot', snapshot())

  await user.click(await screen.findByRole('button', { name: 'Cancelar la clase' }))

  expect(hub().calls('EndSession')).toEqual([[]])
  expect(await screen.findByText('Presentation list')).toBeInTheDocument()
})

test('a dropped connection is announced, keeps the slide on screen and catches up on reconnecting', async () => {
  await openSession()
  hub().receive('SessionSnapshot', snapshot({ status: 'Live', slideIndex: 1 }))
  await screen.findByRole('img', { name: 'Diapositiva 2 de 3' })

  hub().loseConnection()

  expect(await screen.findByRole('status')).toHaveTextContent('Sin conexión. Reconectando…')
  expect(screen.getByRole('img', { name: 'Diapositiva 2 de 3' })).toBeInTheDocument()

  // Back online: the server sends the whole state again.
  hub().receive('SessionSnapshot', snapshot({ status: 'Live', slideIndex: 2 }))

  expect(await screen.findByRole('img', { name: 'Diapositiva 3 de 3' })).toBeInTheDocument()
  expect(screen.queryByText('Sin conexión. Reconectando…')).not.toBeInTheDocument()
})

test('a browser the server refuses is told so and does not keep trying', async () => {
  await openSession()
  const startsBefore = hub().start.mock.calls.length

  hub().closeWith('NotAllowed')

  expect(await screen.findByText('No tenés acceso a esta clase.')).toBeInTheDocument()
  expect(hub().start.mock.calls).toHaveLength(startsBefore)
})

test('when the teacher cookie expired with the tab open, the access key is asked for again', async () => {
  // Signed in when the page opened; by the time the hub refuses, the cookie is gone.
  let signedIn = true
  stubFetch({
    'GET /api/admin/sessions/session-1': jsonResponse(details),
    'GET /api/admin/access': () => new Response(null, { status: signedIn ? 204 : 401 }),
  })
  renderWithQuery(
    <AccessGate>
      <Routes>
        <Route path="/present/:sessionId" element={<PresenterSessionPage />} />
      </Routes>
    </AccessGate>,
    '/present/session-1',
  )
  await waitFor(() => expect(FakeHubConnection.current?.start).toHaveBeenCalled())
  hub().receive('SessionSnapshot', snapshot({ status: 'Live' }))
  await screen.findByRole('img', { name: 'Diapositiva 1 de 3' })

  signedIn = false
  hub().closeWith('NotAllowed')

  expect(await screen.findByLabelText('Clave de acceso')).toBeInTheDocument()
})

test('quick presses with a slow server end on the slide the teacher asked for', async () => {
  const user = await openSession()
  hub().receive('SessionSnapshot', snapshot({ status: 'Live' }))
  hub().holdCalls = true

  await user.click(await screen.findByRole('button', { name: 'Siguiente' }))
  await user.click(screen.getByRole('button', { name: 'Siguiente' }))
  hub().receive('SlideChanged', 1, 0) // the answer to the first press, arriving late

  expect(screen.getByRole('img', { name: 'Diapositiva 3 de 3' })).toBeInTheDocument()
  expect(hub().calls('GoTo')).toEqual([[1, 0], [2, 0]])

  hub().receive('SlideChanged', 2, 0)
  await hub().answerCalls()
  expect(screen.getByRole('img', { name: 'Diapositiva 3 de 3' })).toBeInTheDocument()
})

test('the window to share opens from the lobby and from the class, in this browser', async () => {
  const open = vi.spyOn(window, 'open').mockReturnValue(null)
  const user = await openSession()
  hub().receive('SessionSnapshot', snapshot())

  await user.click(await screen.findByRole('button', { name: 'Abrir ventana para compartir' }))
  expect(open).toHaveBeenLastCalledWith('/display/session-1', 'display-session-1')

  hub().receive('SessionStarted', 0, 0)
  await user.click(await screen.findByRole('button', { name: 'Ventana para compartir' }))
  expect(open).toHaveBeenCalledTimes(2)
})

test('for a projector on another computer, a one-time link is copied to the clipboard', async () => {
  const user = await openSession()
  // Added after the page opened: openSession() set up the stubs for what the page loads first.
  stubFetch({
    'GET /api/admin/sessions/session-1': jsonResponse(details),
    'POST /api/admin/sessions/session-1/display-token': jsonResponse({ token: 'ONE-TIME' }),
  })
  hub().receive('SessionSnapshot', snapshot())

  await user.click(await screen.findByRole('button', { name: 'Copiar enlace para otro equipo' }))

  expect(await screen.findByText(/Enlace copiado\. Sirve una sola vez/)).toBeInTheDocument()
  expect(await navigator.clipboard.readText()).toBe(`${window.location.origin}/display/session-1#token=ONE-TIME`)
})

test('when the browser does not allow copying, the link is shown to be copied by hand', async () => {
  const user = await openSession()
  stubFetch({
    'GET /api/admin/sessions/session-1': jsonResponse(details),
    'POST /api/admin/sessions/session-1/display-token': jsonResponse({ token: 'ONE-TIME' }),
  })
  vi.spyOn(navigator.clipboard, 'writeText').mockRejectedValue(new Error('NotAllowedError'))
  hub().receive('SessionSnapshot', snapshot())

  await user.click(await screen.findByRole('button', { name: 'Copiar enlace para otro equipo' }))

  expect(await screen.findByDisplayValue(`${window.location.origin}/display/session-1#token=ONE-TIME`)).toBeInTheDocument()
  expect(screen.getByText(/Copiá este enlace y abrilo en el otro equipo/)).toBeInTheDocument()
  expect(screen.queryByText(/Enlace copiado/)).not.toBeInTheDocument()
  expect(screen.queryByRole('alert')).not.toBeInTheDocument() // not reported as a failure
})

test('the copied-link notice goes away when the link expires', async () => {
  // Fake clock from the start, so the two-minute timer is created on it. It still follows
  // real time by itself, which keeps everything else in the test working as usual.
  vi.useFakeTimers({ shouldAdvanceTime: true })
  const user = await openSession()
  stubFetch({
    'GET /api/admin/sessions/session-1': jsonResponse(details),
    'POST /api/admin/sessions/session-1/display-token': jsonResponse({ token: 'ONE-TIME' }),
  })
  hub().receive('SessionSnapshot', snapshot())
  await user.click(await screen.findByRole('button', { name: 'Copiar enlace para otro equipo' }))
  await screen.findByText(/Enlace copiado/)

  act(() => {
    vi.advanceTimersByTime(2 * 60 * 1000 - 1000)
  })
  expect(screen.getByText(/Enlace copiado/)).toBeInTheDocument() // still valid, still offered

  act(() => {
    vi.advanceTimersByTime(2000)
  })
  expect(screen.queryByText(/Enlace copiado/)).not.toBeInTheDocument()
})

test('a session that does not exist says so', async () => {
  stubFetch({ 'GET /api/admin/sessions/session-1': jsonResponse({ status: 404 }, 404) })
  renderWithQuery(
    <Routes>
      <Route path="/present/:sessionId" element={<PresenterSessionPage />} />
    </Routes>,
    '/present/session-1',
  )

  expect(await screen.findByText('Esa clase no existe.')).toBeInTheDocument()
  expect(FakeHubConnection.current).toBeUndefined() // nothing to connect to
})

test('leaving the page closes the connection', async () => {
  stubFetch({ 'GET /api/admin/sessions/session-1': jsonResponse(details) })
  const view = renderWithQuery(
    <Routes>
      <Route path="/present/:sessionId" element={<PresenterSessionPage />} />
    </Routes>,
    '/present/session-1',
  )
  await waitFor(() => expect(FakeHubConnection.current?.start).toHaveBeenCalled())

  view.unmount()

  expect(hub().stop).toHaveBeenCalled()
})
