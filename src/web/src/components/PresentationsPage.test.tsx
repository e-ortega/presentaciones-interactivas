import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { expect, test } from 'vitest'
import type { PresentationSummary } from '../api/types'
import { jsonResponse, renderWithQuery, stubFetch } from '../test/render'
import { PresentationsPage } from './PresentationsPage'

function presentation(overrides: Partial<PresentationSummary>): PresentationSummary {
  return {
    id: '01a12436-1dc2-7ed4-a11c-3f767a1a9acc',
    title: 'Semana 01',
    status: 'Ready',
    failure: null,
    slideCount: 16,
    createdAt: '2026-10-10T05:00:00+00:00',
    ...overrides,
  }
}

const pptx = new File(['deck'], 'Semana 02.pptx')

test('lists each presentation with its slide count or its import status', async () => {
  stubFetch({
    'GET /api/admin/presentations': jsonResponse([
      presentation({ id: 'a', title: 'Semana 01', slideCount: 16 }),
      presentation({ id: 'b', title: 'Una sola', slideCount: 1 }),
      presentation({ id: 'c', title: 'Semana 03', status: 'Failed', failure: 'ConversionTimedOut', slideCount: 0 }),
    ]),
  })

  renderWithQuery(<PresentationsPage />)

  const rows = await screen.findAllByRole('listitem')
  expect(rows).toHaveLength(3)
  expect(within(rows[0]).getByText(/16 diapositivas/)).toBeInTheDocument()
  expect(within(rows[0]).getByRole('presentation')).toHaveAttribute('src', '/api/admin/presentations/a/slides/0/steps/0')
  expect(within(rows[1]).getByText(/1 diapositiva ·/)).toBeInTheDocument()
  expect(within(rows[2]).getByText(/No se pudo convertir/)).toBeInTheDocument()
  expect(within(rows[2]).getByText(/La conversión tardó demasiado/)).toBeInTheDocument()
  expect(within(rows[2]).queryByRole('presentation')).not.toBeInTheDocument()
})

test('invites to upload the first deck when there are none', async () => {
  stubFetch({ 'GET /api/admin/presentations': jsonResponse([]) })

  renderWithQuery(<PresentationsPage />)

  expect(await screen.findByText(/Todavía no hay presentaciones/)).toBeInTheDocument()
})

test('uploads the chosen file and shows it in the list while it is converted', async () => {
  const uploaded = presentation({ id: 'new', title: 'Semana 02', status: 'Pending', slideCount: 0 })
  let list: PresentationSummary[] = []
  const fetchMock = stubFetch({
    'GET /api/admin/presentations': () => jsonResponse(list),
    'POST /api/admin/presentations': () => {
      list = [uploaded]
      return jsonResponse(uploaded, 202)
    },
  })
  const user = userEvent.setup()

  renderWithQuery(<PresentationsPage />)
  const submit = screen.getByRole('button', { name: 'Subir presentación' })
  expect(submit).toBeDisabled() // nothing chosen yet
  await user.upload(screen.getByLabelText('Subir un PowerPoint (.pptx)'), pptx)
  await user.click(submit)

  expect(await screen.findByText('Semana 02')).toBeInTheDocument()
  expect(screen.getByText(/En cola para convertir/)).toBeInTheDocument()
  const post = fetchMock.mock.calls.find(([, init]) => init?.method === 'POST')
  const body = post?.[1]?.body
  if (!(body instanceof FormData)) {
    throw new Error('The upload did not send a form.')
  }
  expect(body.get('file')).toEqual(pptx)
})

test('explains why the server rejected a file', async () => {
  stubFetch({
    'GET /api/admin/presentations': jsonResponse([]),
    'POST /api/admin/presentations': jsonResponse({ title: 'The file cannot be imported.', code: 'Unreadable' }, 400),
  })
  const user = userEvent.setup()

  renderWithQuery(<PresentationsPage />)
  await user.upload(screen.getByLabelText('Subir un PowerPoint (.pptx)'), pptx)
  await user.click(screen.getByRole('button', { name: 'Subir presentación' }))

  expect(await screen.findByRole('alert')).toHaveTextContent('No pudimos leer ese archivo como una presentación.')
})
