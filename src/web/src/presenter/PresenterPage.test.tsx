import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Route, Routes } from 'react-router'
import { expect, test } from 'vitest'
import type { PresentationDetail } from '../api/types'
import { jsonResponse, renderWithQuery, stubFetch } from '../test/render'
import { PresenterPage } from './PresenterPage'

const deck: PresentationDetail = {
  id: 'deck-1',
  title: 'Semana 01',
  status: 'Ready',
  failure: null,
  createdAt: '2026-10-10T05:00:00+00:00',
  slides: [
    { index: 0, speakerNotes: 'Saludar a la clase.', stepCount: 1 },
    { index: 1, speakerNotes: 'Explicar qué es un ERP.\nPreguntar ejemplos.', stepCount: 1 },
    { index: 2, speakerNotes: '', stepCount: 1 },
  ],
}

function renderPresenter(response: Response = jsonResponse(deck)) {
  stubFetch({ 'GET /api/admin/presentations/deck-1': response })
  renderWithQuery(
    <Routes>
      <Route path="/admin/presentations/:presentationId/present" element={<PresenterPage />} />
      <Route path="/admin" element={<p>Presentation list</p>} />
    </Routes>,
    '/admin/presentations/deck-1/present',
  )
  return userEvent.setup()
}

const slideImage = (number: number) => screen.findByRole('img', { name: `Diapositiva ${number} de 3` })
const script = () => screen.getByRole('complementary', { name: 'Guion' })

test('opens on the first slide with its notes, and the next notes after them', async () => {
  renderPresenter()

  expect(await slideImage(1)).toHaveAttribute('src', '/api/admin/presentations/deck-1/slides/0/steps/0')
  expect(script()).toHaveTextContent('Saludar a la clase.')
  expect(script()).toHaveTextContent('Explicar qué es un ERP.')
  expect(screen.getByText('1 / 3')).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Anterior' })).toBeDisabled()
})

test('the buttons move forward and back, and stop at the ends', async () => {
  const user = renderPresenter()
  await slideImage(1)

  await user.click(screen.getByRole('button', { name: 'Siguiente' }))
  expect(await slideImage(2)).toHaveAttribute('src', '/api/admin/presentations/deck-1/slides/1/steps/0')
  expect(script()).toHaveTextContent('Explicar qué es un ERP.')

  await user.click(screen.getByRole('button', { name: 'Siguiente' }))
  expect(await slideImage(3)).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Siguiente' })).toBeDisabled()

  await user.click(screen.getByRole('button', { name: 'Anterior' }))
  expect(await slideImage(2)).toBeInTheDocument()
})

test('the keyboard moves through the deck like a presentation remote', async () => {
  const user = renderPresenter()
  await slideImage(1)

  await user.keyboard('{ArrowRight}')
  expect(await slideImage(2)).toBeInTheDocument()
  await user.keyboard('{PageDown}')
  expect(await slideImage(3)).toBeInTheDocument()
  await user.keyboard('{ArrowRight}') // already at the end
  expect(screen.getByText('3 / 3')).toBeInTheDocument()
  await user.keyboard('{ArrowLeft}')
  expect(await slideImage(2)).toBeInTheDocument()
  await user.keyboard('{Home}')
  expect(await slideImage(1)).toBeInTheDocument()
  await user.keyboard('{End}')
  expect(await slideImage(3)).toBeInTheDocument()
})

test('the space bar on a focused button presses that button and nothing else', async () => {
  const user = renderPresenter()
  await slideImage(1)

  screen.getByRole('button', { name: 'Siguiente' }).focus()
  await user.keyboard(' ')

  // One step forward from the button; the key itself must not add a second one.
  expect(await slideImage(2)).toBeInTheDocument()
  expect(screen.getByText('2 / 3')).toBeInTheDocument()
})

test('a slide without notes says so instead of showing an empty column', async () => {
  const user = renderPresenter()
  await slideImage(1)

  await user.keyboard('{End}')

  await slideImage(3)
  expect(script()).toHaveTextContent('Esta diapositiva no tiene notas.')
})

test('the script can be hidden to give the slide the whole window, and shown again', async () => {
  const user = renderPresenter()
  await slideImage(1)
  const toggle = screen.getByRole('button', { name: 'Guion' })
  expect(toggle).toHaveAttribute('aria-pressed', 'true')

  await user.click(toggle)
  expect(screen.queryByRole('complementary', { name: 'Guion' })).not.toBeInTheDocument()
  expect(screen.queryByRole('button', { name: 'Notas fijas' })).not.toBeInTheDocument()

  await user.click(toggle)
  expect(script()).toHaveTextContent('Saludar a la clase.')
})

test('static notes list the whole deck and mark the slide on screen', async () => {
  const user = renderPresenter()
  await slideImage(1)

  await user.click(screen.getByRole('button', { name: 'Notas fijas' }))

  const notes = within(script()).getAllByRole('listitem')
  expect(notes).toHaveLength(3)
  expect(notes[0]).toHaveAttribute('aria-current', 'true')
  expect(notes[1]).toHaveTextContent('Explicar qué es un ERP.')

  await user.keyboard('{ArrowRight}')
  await slideImage(2)
  expect(notes[0]).not.toHaveAttribute('aria-current')
  expect(notes[1]).toHaveAttribute('aria-current', 'true')
})

test('the next image is loaded ahead of time', async () => {
  renderPresenter()
  await slideImage(1)

  const images = Array.from(document.querySelectorAll('img')).map((image) => image.getAttribute('src'))
  expect(images).toContain('/api/admin/presentations/deck-1/slides/1/steps/0')
})

test('exit goes back to the presentation list', async () => {
  const user = renderPresenter()
  await slideImage(1)

  await user.click(screen.getByRole('link', { name: 'Salir' }))

  expect(screen.getByText('Presentation list')).toBeInTheDocument()
})

test('a presentation that is still being converted cannot be presented yet', async () => {
  renderPresenter(jsonResponse({ ...deck, status: 'Processing', slides: [] }))

  expect(await screen.findByText('Esta presentación todavía no está lista para presentar.')).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'Volver a mis presentaciones' })).toHaveAttribute('href', '/admin')
})

test('an address for a presentation that does not exist says so', async () => {
  renderPresenter(jsonResponse({ title: 'Not Found', status: 404 }, 404))

  expect(await screen.findByText('Esa presentación no existe.')).toBeInTheDocument()
})
