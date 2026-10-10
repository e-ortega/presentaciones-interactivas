import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, expect, test, vi } from 'vitest'
import { FakeHubConnection, hub } from '../test/fakeHub'
import type { SessionSnapshot } from './contracts'
import { useLiveSession } from './useLiveSession'

vi.mock('@microsoft/signalr', async () => (await import('../test/fakeHub')).fakeSignalR)

const live: SessionSnapshot = {
  sessionId: 'session-1',
  status: 'Live',
  title: 'Semana 01',
  slideIndex: 0,
  stepIndex: 0,
  stepCounts: [1, 1, 1, 1],
  code: '88886666',
  attendance: { connectedCount: 0, attendees: [] },
}

beforeEach(() => {
  FakeHubConnection.current = undefined
})

/** Uses the hook as a page would and waits until it has connected. */
async function connect(role: 'Presenter' | 'Participant' = 'Presenter') {
  const rendered = renderHook(() => useLiveSession('session-1', role))
  await waitFor(() => expect(FakeHubConnection.current?.start).toHaveBeenCalled())
  return rendered
}

const position = (session: SessionSnapshot | null) => session && [session.slideIndex, session.stepIndex]

test('quick presses on a slow network are not undone by the answers that arrive late', async () => {
  const { result } = await connect()
  hub().receive('SessionSnapshot', live)
  hub().holdCalls = true

  // Three presses before the server has answered the first.
  act(() => void result.current.goTo({ slideIndex: 1, stepIndex: 0 }))
  act(() => void result.current.goTo({ slideIndex: 2, stepIndex: 0 }))
  act(() => void result.current.goTo({ slideIndex: 3, stepIndex: 0 }))
  expect(position(result.current.session)).toEqual([3, 0])

  // The echo of the first press arrives: the screen must stay where the teacher is.
  hub().receive('SlideChanged', 1, 0)
  expect(position(result.current.session)).toEqual([3, 0])

  hub().receive('SlideChanged', 2, 0)
  hub().receive('SlideChanged', 3, 0)
  await hub().answerCalls()

  expect(position(result.current.session)).toEqual([3, 0])
  expect(hub().calls('GoTo')).toEqual([[1, 0], [2, 0], [3, 0]])
})

test('a slide change the server refuses falls back to where the server says the class is', async () => {
  const { result } = await connect()
  hub().receive('SessionSnapshot', { ...live, slideIndex: 2 })
  hub().refuse = 'GoTo'

  let failed = false
  await act(async () => {
    await result.current.goTo({ slideIndex: 3, stepIndex: 0 }).catch(() => {
      failed = true
    })
  })

  expect(failed).toBe(true)
  expect(position(result.current.session)).toEqual([2, 0])
})

test('a participant who is removed stays out: the connection is closed and not reopened', async () => {
  const { result } = await connect('Participant')
  hub().receive('SessionSnapshot', { ...live, code: null, attendance: null })
  const startsBefore = hub().start.mock.calls.length

  hub().receive('RemovedFromSession')
  await new Promise((resolve) => setTimeout(resolve, 50))

  expect(result.current.connection).toBe('refused')
  expect(result.current.refusal).toBe('Removed')
  expect(hub().stop).toHaveBeenCalled()
  expect(hub().start.mock.calls).toHaveLength(startsBefore)
})

test('a connection the server closes without refusing it is reopened, after a pause', async () => {
  const { result } = await connect()
  hub().receive('SessionSnapshot', live)
  const startsBefore = hub().start.mock.calls.length

  hub().closeUnexpectedly()

  expect(result.current.connection).toBe('reconnecting')
  // Not at once: a server that fails right after connecting must not be hammered.
  await new Promise((resolve) => setTimeout(resolve, 300))
  expect(hub().start.mock.calls).toHaveLength(startsBefore)
  await waitFor(() => expect(hub().start.mock.calls).toHaveLength(startsBefore + 1), { timeout: 3000 })
})

test('an expired cookie found while reconnecting ends the attempts and is reported as a refusal', async () => {
  const { result } = await connect()
  hub().receive('SessionSnapshot', live)
  hub().loseConnection()
  const nextDelay = hub().retryPolicy!.nextRetryDelayInMilliseconds

  // A network error is retried, with growing pauses and without ever giving up.
  const networkError = new Error('Failed to fetch')
  expect(nextDelay({ previousRetryCount: 0, elapsedMilliseconds: 0, retryReason: networkError })).toBe(0)
  expect(nextDelay({ previousRetryCount: 2, elapsedMilliseconds: 3000, retryReason: networkError })).toBe(2000)
  expect(nextDelay({ previousRetryCount: 50, elapsedMilliseconds: 500000, retryReason: networkError })).toBe(10000)
  expect(result.current.connection).toBe('reconnecting')

  // A 401 is not: retrying would get the same answer.
  const unauthorized = new Error("Failed to complete negotiation with the server: Error: Unauthorized: Status code '401'")
  let delay: number | null = 0
  act(() => {
    delay = nextDelay({ previousRetryCount: 3, elapsedMilliseconds: 9000, retryReason: unauthorized })
  })

  expect(delay).toBeNull()
  expect(result.current.connection).toBe('refused')
  expect(result.current.refusal).toBe('NotAllowed')
})

test('the server refusing the connection is reported with its reason', async () => {
  const { result } = await connect('Participant')

  hub().closeWith('Removed')

  expect(result.current.connection).toBe('refused')
  expect(result.current.refusal).toBe('Removed')
})

test('leaving the page closes the connection and nothing reopens it', async () => {
  const { unmount } = await connect()
  hub().receive('SessionSnapshot', live)
  const startsBefore = hub().start.mock.calls.length

  unmount()
  await new Promise((resolve) => setTimeout(resolve, 50))

  expect(hub().stop).toHaveBeenCalled()
  expect(hub().start.mock.calls).toHaveLength(startsBefore)
})
