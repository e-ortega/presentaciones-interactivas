import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { expect, test } from 'vitest'
import { renderWithQuery, stubFetch } from '../test/render'
import { AccessGate } from './AccessGate'

function renderGate() {
  renderWithQuery(
    <AccessGate>
      <p>Teacher area</p>
    </AccessGate>,
  )
}

test('shows the teacher area when the browser already has access', async () => {
  stubFetch({ 'GET /api/admin/access': new Response(null, { status: 204 }) })

  renderGate()

  expect(await screen.findByText('Teacher area')).toBeInTheDocument()
})

test('asks for the key first, and opens the teacher area once it is accepted', async () => {
  const fetchMock = stubFetch({
    'GET /api/admin/access': new Response(null, { status: 401 }),
    'POST /api/access': new Response(null, { status: 204 }),
  })
  const user = userEvent.setup()

  renderGate()
  await user.type(await screen.findByLabelText('Clave de acceso'), 'my-key')
  expect(screen.queryByText('Teacher area')).not.toBeInTheDocument()
  await user.click(screen.getByRole('button', { name: 'Entrar' }))

  expect(await screen.findByText('Teacher area')).toBeInTheDocument()
  expect(fetchMock).toHaveBeenCalledWith('/api/access', expect.objectContaining({ body: JSON.stringify({ key: 'my-key' }) }))
})

test('says the key is wrong and keeps the teacher area closed', async () => {
  stubFetch({
    'GET /api/admin/access': new Response(null, { status: 401 }),
    'POST /api/access': new Response(null, { status: 401 }),
  })
  const user = userEvent.setup()

  renderGate()
  await user.type(await screen.findByLabelText('Clave de acceso'), 'wrong')
  await user.click(screen.getByRole('button', { name: 'Entrar' }))

  expect(await screen.findByRole('alert')).toHaveTextContent('Esa clave no es correcta.')
  expect(screen.queryByText('Teacher area')).not.toBeInTheDocument()
})

test('explains the wait when there were too many attempts', async () => {
  stubFetch({
    'GET /api/admin/access': new Response(null, { status: 401 }),
    'POST /api/access': new Response(null, { status: 429 }),
  })
  const user = userEvent.setup()

  renderGate()
  await user.type(await screen.findByLabelText('Clave de acceso'), 'any')
  await user.click(screen.getByRole('button', { name: 'Entrar' }))

  expect(await screen.findByRole('alert')).toHaveTextContent('Demasiados intentos.')
})
