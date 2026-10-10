import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { expect, test } from 'vitest'
import App from './App'
import { jsonResponse, renderWithQuery, stubFetch } from './test/render'

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

test('the home address leads to the teacher area', async () => {
  stubFetch({
    'GET /api/admin/access': access,
    'GET /health': healthy,
    'GET /api/admin/presentations': jsonResponse([]),
  })

  renderWithQuery(<App />, '/')

  expect(await screen.findByRole('heading', { name: 'Mis presentaciones' })).toBeInTheDocument()
})

test('"Presentar" opens the presenter view of that presentation', async () => {
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
  await user.click(await screen.findByRole('link', { name: 'Presentar' }))

  expect(await screen.findByRole('img', { name: 'Diapositiva 1 de 2' })).toBeInTheDocument()
  expect(screen.getByRole('complementary', { name: 'Guion' })).toHaveTextContent('Primera nota')
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
