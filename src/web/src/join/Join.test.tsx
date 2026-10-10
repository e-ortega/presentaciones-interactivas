import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Route, Routes } from 'react-router'
import { expect, test } from 'vitest'
import { jsonResponse, renderWithQuery, stubFetch } from '../test/render'
import { JoinCodePage } from './JoinCodePage'
import { JoinFormPage } from './JoinFormPage'

const joinInfo = jsonResponse({ title: 'Semana 01', noticeVersion: '1.0' })

function renderJoin(path: string) {
  renderWithQuery(
    <Routes>
      <Route path="/" element={<JoinCodePage />} />
      <Route path="/s/:code" element={<JoinFormPage />} />
      <Route path="/session/:sessionId" element={<p>In the class</p>} />
    </Routes>,
    path,
  )
  return userEvent.setup()
}

async function fillIn(user: ReturnType<typeof userEvent.setup>, name: string, email: string) {
  await user.type(await screen.findByLabelText('Tu nombre'), name)
  await user.type(screen.getByLabelText('Tu correo'), email)
}

test('a code typed with its dash leads to that class', async () => {
  stubFetch({ 'GET /api/join/88886666': joinInfo })
  const user = renderJoin('/')

  await user.type(screen.getByLabelText('Código de la clase'), '8888-6666')
  await user.click(screen.getByRole('button', { name: 'Continuar' }))

  expect(await screen.findByRole('heading', { name: 'Semana 01' })).toBeInTheDocument()
})

test('something that is not a code is caught before asking the server', async () => {
  const fetchMock = stubFetch({})
  const user = renderJoin('/')

  await user.type(screen.getByLabelText('Código de la clase'), '1234')
  await user.click(screen.getByRole('button', { name: 'Continuar' }))

  expect(screen.getByRole('alert')).toHaveTextContent('El código tiene 8 números')
  expect(fetchMock).not.toHaveBeenCalled()

  await user.type(screen.getByLabelText('Código de la clase'), '5')
  expect(screen.queryByRole('alert')).not.toBeInTheDocument() // typing again clears the message
})

test('a code that opens no class says so and offers another try', async () => {
  stubFetch({ 'GET /api/join/00000000': jsonResponse({ code: 'SessionNotFound' }, 404) })
  const user = renderJoin('/s/00000000')

  expect(await screen.findByRole('alert')).toHaveTextContent('Ese código no corresponde a una clase abierta')

  await user.click(screen.getByRole('link', { name: 'Probar con otro código' }))
  expect(screen.getByRole('heading', { name: 'Unirme a una clase' })).toBeInTheDocument()
})

test('the privacy notice comes before the fields and says who will see what', async () => {
  stubFetch({ 'GET /api/join/88886666': joinInfo })
  renderJoin('/s/88886666')

  const notice = await screen.findByText(/para registrar tu participación en esta clase guardamos/)
  expect(notice).toHaveTextContent('Tu nombre lo verán tu profesor o profesora y las demás personas de la sesión')
  expect(notice).toHaveTextContent('tu correo y tus respuestas, solo tu profesor o profesora')
  expect(notice.compareDocumentPosition(screen.getByLabelText('Tu nombre')) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  expect(screen.getByText('Aviso de privacidad, versión 1.0')).toBeInTheDocument()
})

test('coming back to the form with the network down does not take the form away', async () => {
  let online = true
  stubFetch({
    'GET /api/join/88886666': () => (online ? jsonResponse({ title: 'Semana 01', noticeVersion: '1.0' }) : new Response(null, { status: 503 })),
  })
  const user = renderJoin('/s/88886666')
  await fillIn(user, 'Ana Mora', 'ana@example.com')

  // The student switches to another app and back while the connection is failing.
  online = false
  window.dispatchEvent(new Event('focus'))
  document.dispatchEvent(new Event('visibilitychange'))
  await new Promise((resolve) => setTimeout(resolve, 50))

  expect(screen.getByLabelText('Tu nombre')).toHaveValue('Ana Mora')
  expect(screen.queryByText(/No se pudo buscar la clase/)).not.toBeInTheDocument()
})

test('consent starts unticked, and nobody joins without ticking it', async () => {
  const fetchMock = stubFetch({ 'GET /api/join/88886666': joinInfo })
  const user = renderJoin('/s/88886666')
  await fillIn(user, 'Ana Mora', 'ana@example.com')

  const consent = screen.getByRole('checkbox', { name: /Acepto que .* trate mis datos/ })
  const submit = screen.getByRole('button', { name: 'Unirme a la clase' })
  expect(consent).not.toBeChecked()
  expect(submit).toBeDisabled()

  await user.click(submit)
  expect(fetchMock).toHaveBeenCalledTimes(1) // only the lookup of the class
})

test('with name, email and consent the student joins and goes into the class', async () => {
  const fetchMock = stubFetch({
    'GET /api/join/88886666': joinInfo,
    'POST /api/join/88886666': jsonResponse({ sessionId: 'session-1' }),
  })
  const user = renderJoin('/s/88886666')
  await fillIn(user, 'Ana Mora', 'ana@example.com')

  await user.click(screen.getByRole('checkbox'))
  await user.click(screen.getByRole('button', { name: 'Unirme a la clase' }))

  expect(await screen.findByText('In the class')).toBeInTheDocument()
  expect(fetchMock).toHaveBeenCalledWith(
    '/api/join/88886666',
    expect.objectContaining({ body: JSON.stringify({ name: 'Ana Mora', email: 'ana@example.com', consent: true }) }),
  )
})

test.each([
  ['EmailInvalid', 400, 'Ese correo no parece válido. Revisalo.'],
  ['Removed', 403, 'Tu profesor o profesora te sacó de esta clase.'],
  ['SessionNotFound', 404, 'Esa clase ya no está abierta.'],
  ['SomethingNew', 400, 'No se pudo entrar a la clase. Probá de nuevo.'],
])('the server refusing with %s is explained in plain words', async (code, status, message) => {
  stubFetch({
    'GET /api/join/88886666': joinInfo,
    'POST /api/join/88886666': jsonResponse({ code }, status),
  })
  const user = renderJoin('/s/88886666')
  await fillIn(user, 'Ana Mora', 'ana@example.com')
  await user.click(screen.getByRole('checkbox'))

  await user.click(screen.getByRole('button', { name: 'Unirme a la clase' }))

  expect(await screen.findByText(message)).toBeInTheDocument()
  expect(screen.queryByText('In the class')).not.toBeInTheDocument()
})

test('too many attempts from the same network is explained too', async () => {
  stubFetch({
    'GET /api/join/88886666': joinInfo,
    'POST /api/join/88886666': new Response(null, { status: 429 }),
  })
  const user = renderJoin('/s/88886666')
  await fillIn(user, 'Ana Mora', 'ana@example.com')
  await user.click(screen.getByRole('checkbox'))

  await user.click(screen.getByRole('button', { name: 'Unirme a la clase' }))

  expect(await screen.findByText(/Demasiados intentos/)).toBeInTheDocument()
})
