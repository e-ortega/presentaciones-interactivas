import { screen, waitFor } from '@testing-library/react'
import { Route, Routes } from 'react-router'
import { beforeEach, expect, test, vi } from 'vitest'
import type { SessionSnapshot } from '../realtime/contracts'
import { FakeHubConnection, hub } from '../test/fakeHub'
import { jsonResponse, renderWithQuery, stubFetch } from '../test/render'
import { DisplayPage } from './DisplayPage'
import { StudentSessionPage } from './StudentSessionPage'

vi.mock('@microsoft/signalr', async () => (await import('../test/fakeHub')).fakeSignalR)

beforeEach(() => {
  FakeHubConnection.current = undefined
})

/** What the server sends a student: no code and no attendance list. */
function forStudent(overrides: Partial<SessionSnapshot> = {}): SessionSnapshot {
  return {
    sessionId: 'session-1',
    status: 'Lobby',
    title: 'Semana 01',
    slideIndex: 0,
    stepIndex: 0,
    stepCounts: [1, 1, 1],
    code: null,
    attendance: null,
    ...overrides,
  }
}

/** What the server sends the display window: the code and who is in. */
function forDisplay(overrides: Partial<SessionSnapshot> = {}): SessionSnapshot {
  return {
    ...forStudent(),
    code: '88886666',
    attendance: {
      connectedCount: 2,
      attendees: [
        { attendanceId: 'a1', displayName: 'Ana Mora', connected: true },
        { attendanceId: 'a2', displayName: 'Luis Vega', connected: true },
      ],
    },
    ...overrides,
  }
}

async function open(path: string) {
  renderWithQuery(
    <Routes>
      <Route path="/session/:sessionId" element={<StudentSessionPage />} />
      <Route path="/display/:sessionId" element={<DisplayPage />} />
      <Route path="/" element={<p>Join page</p>} />
    </Routes>,
    path,
  )
  await waitFor(() => expect(FakeHubConnection.current?.start).toHaveBeenCalled())
}

// --- Student ---------------------------------------------------------------------------------

test('a student who joined waits in the lobby and is told the screen will change by itself', async () => {
  await open('/session/session-1')
  expect(hub().url).toBe('/hubs/session?sessionId=session-1&role=Participant')

  hub().receive('SessionSnapshot', forStudent())

  expect(await screen.findByRole('heading', { name: 'Ya estás dentro' })).toBeInTheDocument()
  expect(screen.getByText('Semana 01')).toBeInTheDocument()
  expect(screen.getByText(/Esperando a que empiece la clase/)).toBeInTheDocument()
  expect(screen.queryByRole('img')).not.toBeInTheDocument()
})

test('when the class starts the student sees the slide, and follows every change', async () => {
  await open('/session/session-1')
  hub().receive('SessionSnapshot', forStudent())

  hub().receive('SessionStarted', 0, 0)
  expect(await screen.findByRole('img', { name: 'Diapositiva 1 de 3' })).toHaveAttribute(
    'src',
    '/api/sessions/session-1/slides/0/steps/0',
  )

  hub().receive('SlideChanged', 2, 0)
  expect(await screen.findByRole('img', { name: 'Diapositiva 3 de 3' })).toHaveAttribute(
    'src',
    '/api/sessions/session-1/slides/2/steps/0',
  )
})

test('the student has nothing to move the slides with', async () => {
  await open('/session/session-1')
  hub().receive('SessionSnapshot', forStudent({ status: 'Live', slideIndex: 1 }))
  await screen.findByRole('img', { name: 'Diapositiva 2 de 3' })

  expect(screen.queryByRole('button')).not.toBeInTheDocument()
  expect(screen.queryByRole('link')).not.toBeInTheDocument()

  // Keys do nothing either: the view is tied to the teacher's.
  window.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight' }))
  expect(screen.getByRole('img', { name: 'Diapositiva 2 de 3' })).toBeInTheDocument()
  expect(hub().invocations).toEqual([])
})

test('a student who arrives late or reloads gets the slide the class is on', async () => {
  await open('/session/session-1')

  hub().receive('SessionSnapshot', forStudent({ status: 'Live', slideIndex: 2 }))

  expect(await screen.findByRole('img', { name: 'Diapositiva 3 de 3' })).toBeInTheDocument()
})

test('a dropped connection is announced and the slide stays until it comes back', async () => {
  await open('/session/session-1')
  hub().receive('SessionSnapshot', forStudent({ status: 'Live', slideIndex: 1 }))
  await screen.findByRole('img', { name: 'Diapositiva 2 de 3' })

  hub().loseConnection()
  expect(await screen.findByRole('status')).toHaveTextContent('Sin conexión. Reconectando…')
  expect(screen.getByRole('img', { name: 'Diapositiva 2 de 3' })).toBeInTheDocument()

  hub().receive('SessionSnapshot', forStudent({ status: 'Live', slideIndex: 2 }))
  expect(await screen.findByRole('img', { name: 'Diapositiva 3 de 3' })).toBeInTheDocument()
  expect(screen.queryByRole('status')).not.toBeInTheDocument()
})

test('the end of the class closes the view with thanks', async () => {
  await open('/session/session-1')
  hub().receive('SessionSnapshot', forStudent({ status: 'Live' }))

  hub().receive('SessionEnded')

  expect(await screen.findByRole('heading', { name: 'La clase terminó' })).toBeInTheDocument()
  expect(screen.queryByRole('img')).not.toBeInTheDocument()
})

test('a student the teacher removes is told so and the slide goes away', async () => {
  await open('/session/session-1')
  hub().receive('SessionSnapshot', forStudent({ status: 'Live' }))
  await screen.findByRole('img', { name: 'Diapositiva 1 de 3' })

  hub().receive('RemovedFromSession')

  expect(await screen.findByRole('alert')).toHaveTextContent('Tu profesor o profesora te sacó de esta clase.')
  expect(screen.queryByRole('img')).not.toBeInTheDocument()
  expect(screen.queryByRole('link')).not.toBeInTheDocument() // no way back in is offered
})

test('opening a class without having joined leads back to the code', async () => {
  await open('/session/session-1')

  hub().closeWith('NotAllowed')

  expect(await screen.findByRole('alert')).toHaveTextContent('Para ver esta clase hay que entrar con su código.')
  expect(screen.getByRole('link', { name: 'Entrar con un código' })).toHaveAttribute('href', '/')
})

// --- Window to share --------------------------------------------------------------------------

test('the window to share opens on the waiting room: the code, a QR code and who has joined', async () => {
  await open('/display/session-1')
  expect(hub().url).toBe('/hubs/session?sessionId=session-1&role=Display')

  hub().receive('SessionSnapshot', forDisplay())

  expect(await screen.findByText('8888-6666')).toBeInTheDocument()
  expect(screen.getByRole('heading', { name: `Entrá a ${window.location.host} con este código` })).toBeInTheDocument()
  expect(screen.getByTitle('Código QR para unirse a la clase')).toBeInTheDocument()
  expect(screen.getByRole('heading', { name: 'Lista de asistencia · 2 personas' })).toBeInTheDocument()
  expect(screen.getAllByRole('listitem').map((item) => item.textContent)).toEqual(['Ana Mora', 'Luis Vega'])
})

test('during the class it shows the slide and nothing else: no notes, no controls, no names', async () => {
  await open('/display/session-1')
  hub().receive('SessionSnapshot', forDisplay())

  hub().receive('SessionStarted', 0, 0)
  hub().receive('SlideChanged', 1, 0)

  expect(await screen.findByRole('img', { name: 'Diapositiva 2 de 3' })).toHaveAttribute(
    'src',
    '/api/sessions/session-1/slides/1/steps/0',
  )
  expect(screen.queryByRole('button')).not.toBeInTheDocument()
  expect(screen.queryByText('Ana Mora')).not.toBeInTheDocument()
  expect(screen.queryByText('8888-6666')).not.toBeInTheDocument()
})

test('the waiting room fills in as students join', async () => {
  await open('/display/session-1')
  hub().receive('SessionSnapshot', forDisplay({ attendance: { connectedCount: 0, attendees: [] } }))
  expect(await screen.findByRole('heading', { name: 'Lista de asistencia · 0 personas' })).toBeInTheDocument()

  hub().receive('AttendanceChanged', {
    connectedCount: 1,
    attendees: [{ attendanceId: 'a1', displayName: 'Ana Mora', connected: true }],
  })

  expect(await screen.findByText('Ana Mora')).toBeInTheDocument()
  expect(screen.getByRole('heading', { name: 'Lista de asistencia · 1 persona' })).toBeInTheDocument()
})

test('on another computer the one-time link is traded for access before connecting, and leaves the address bar', async () => {
  const fetchMock = stubFetch({ 'POST /api/display/access': jsonResponse({ sessionId: 'session-1' }) })
  window.history.replaceState(null, '', '/display/session-1#token=ABC123')

  await open('/display/session-1#token=ABC123')

  expect(fetchMock).toHaveBeenCalledWith('/api/display/access', expect.objectContaining({ body: JSON.stringify({ token: 'ABC123' }) }))
  await waitFor(() => expect(window.location.hash).toBe(''))
  hub().receive('SessionSnapshot', forDisplay())
  expect(await screen.findByText('8888-6666')).toBeInTheDocument()
})

test('a link that was already used says so and does not connect', async () => {
  stubFetch({ 'POST /api/display/access': jsonResponse({ code: 'InvalidDisplayToken' }, 400) })

  renderWithQuery(
    <Routes>
      <Route path="/display/:sessionId" element={<DisplayPage />} />
    </Routes>,
    '/display/session-1#token=USED',
  )

  expect(await screen.findByRole('alert')).toHaveTextContent('Este enlace ya no sirve.')
  expect(FakeHubConnection.current).toBeUndefined()
})

test('a display window that lost its access says how to open it again', async () => {
  await open('/display/session-1')

  hub().closeWith('NotAllowed')

  expect(await screen.findByRole('alert')).toHaveTextContent('Abrila de nuevo desde la vista del presentador.')
})
