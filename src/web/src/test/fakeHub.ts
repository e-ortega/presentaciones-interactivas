import { act } from '@testing-library/react'
import { vi } from 'vitest'

type Handler = (...args: never[]) => void

type RetryPolicy = {
  nextRetryDelayInMilliseconds: (retry: {
    previousRetryCount: number
    elapsedMilliseconds: number
    retryReason: Error
  }) => number | null
}

/**
 * Stands in for a SignalR connection in tests: records what the page asks of the server and
 * lets the test play the server's part. Use it by mocking the library in the test file:
 *
 *   vi.mock('@microsoft/signalr', async () => (await import('../test/fakeHub')).fakeSignalR)
 */
export class FakeHubConnection {
  /** The connection the page opened most recently. */
  static current: FakeHubConnection | undefined

  url: string
  state = 'Disconnected'
  /** Every call the page made to the server, in order. */
  invocations: { method: string; args: unknown[] }[] = []
  /** Set to make the server refuse the calls to a method. */
  refuse: string | null = null
  /** Set to leave calls unanswered, as on a slow network, until `answerCalls()`. */
  holdCalls = false
  /** What the page told SignalR to do when the connection drops. */
  retryPolicy: RetryPolicy | undefined

  private handlers = new Map<string, Handler[]>()
  private closeHandlers: ((error?: Error) => void)[] = []
  private reconnectingHandlers: (() => void)[] = []
  private heldCalls: (() => void)[] = []

  constructor(url: string) {
    this.url = url
  }

  start = vi.fn(() => {
    this.state = 'Connected'
    return Promise.resolve()
  })

  /** Like the real one: stopping an open connection reports it closed, with no error. */
  stop = vi.fn(() => {
    const wasOpen = this.state === 'Connected'
    this.state = 'Disconnected'
    if (wasOpen) {
      this.closeHandlers.forEach((handler) => handler())
    }
    return Promise.resolve()
  })

  on(message: string, handler: Handler) {
    this.handlers.set(message, [...(this.handlers.get(message) ?? []), handler])
  }

  onclose(handler: (error?: Error) => void) {
    this.closeHandlers.push(handler)
  }

  onreconnecting(handler: () => void) {
    this.reconnectingHandlers.push(handler)
  }

  onreconnected() {
    // The page relies on the snapshot the server sends after reconnecting, not on this event.
  }

  invoke(method: string, ...args: unknown[]): Promise<void> {
    this.invocations.push({ method, args })
    const refused = this.refuse === method
    const answer = () => (refused ? Promise.reject(new Error('InvalidState')) : Promise.resolve())
    if (!this.holdCalls) {
      return answer()
    }
    return new Promise<void>((resolve, reject) => {
      this.heldCalls.push(() => void answer().then(resolve, reject))
    })
  }

  // --- The test playing the server ---------------------------------------------------------

  /** The server sends a message to this client. */
  receive(message: string, ...args: unknown[]) {
    act(() => {
      for (const handler of this.handlers.get(message) ?? []) {
        Reflect.apply(handler, undefined, args)
      }
    })
  }

  /** The server answers the calls left waiting by `holdCalls`. */
  async answerCalls() {
    const waiting = this.heldCalls
    this.heldCalls = []
    await act(async () => {
      waiting.forEach((answer) => answer())
      await Promise.resolve()
    })
  }

  /** The network drops and SignalR starts trying to reconnect. */
  loseConnection() {
    act(() => this.reconnectingHandlers.forEach((handler) => handler()))
  }

  /** The server closes the connection for good, giving a reason. */
  closeWith(reason: string) {
    this.close(new Error(`Connection closed with an error. HubException: ${reason}`))
  }

  /** The connection closes for a reason that is not a refusal, such as a server failure. */
  closeUnexpectedly() {
    this.close(new Error('Connection closed with an error.'))
  }

  /** The calls the page made to one method. */
  calls(method: string): unknown[][] {
    return this.invocations.filter((invocation) => invocation.method === method).map((invocation) => invocation.args)
  }

  private close(error: Error) {
    this.state = 'Disconnected'
    act(() => this.closeHandlers.forEach((handler) => handler(error)))
  }
}

/** The connection the page has open. Fails the test if it never opened one. */
export function hub(): FakeHubConnection {
  if (FakeHubConnection.current === undefined) {
    throw new Error('The page has not opened a connection to the hub.')
  }
  return FakeHubConnection.current
}

class FakeHubConnectionBuilder {
  private url = ''
  private retryPolicy: RetryPolicy | undefined

  withUrl(url: string) {
    this.url = url
    return this
  }

  withAutomaticReconnect(policy?: RetryPolicy) {
    this.retryPolicy = policy
    return this
  }

  build() {
    FakeHubConnection.current = new FakeHubConnection(this.url)
    FakeHubConnection.current.retryPolicy = this.retryPolicy
    return FakeHubConnection.current
  }
}

/** What `@microsoft/signalr` is replaced with. */
export const fakeSignalR = {
  HubConnectionBuilder: FakeHubConnectionBuilder,
  HubConnectionState: { Disconnected: 'Disconnected', Connected: 'Connected' },
}
