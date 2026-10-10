import { act } from '@testing-library/react'
import { vi } from 'vitest'

type Handler = (...args: never[]) => void

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

  private handlers = new Map<string, Handler[]>()
  private closeHandlers: ((error?: Error) => void)[] = []
  private reconnectingHandlers: (() => void)[] = []

  constructor(url: string) {
    this.url = url
  }

  start = vi.fn(() => {
    this.state = 'Connected'
    return Promise.resolve()
  })

  stop = vi.fn(() => {
    this.state = 'Disconnected'
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
    return this.refuse === method ? Promise.reject(new Error('InvalidState')) : Promise.resolve()
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

  /** The network drops and SignalR starts trying to reconnect. */
  loseConnection() {
    act(() => this.reconnectingHandlers.forEach((handler) => handler()))
  }

  /** The server closes the connection for good, giving a reason. */
  closeWith(reason: string) {
    this.state = 'Disconnected'
    act(() => this.closeHandlers.forEach((handler) => handler(new Error(`Connection closed with an error. HubException: ${reason}`))))
  }

  /** The calls the page made to one method. */
  calls(method: string): unknown[][] {
    return this.invocations.filter((invocation) => invocation.method === method).map((invocation) => invocation.args)
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

  withUrl(url: string) {
    this.url = url
    return this
  }

  withAutomaticReconnect() {
    return this
  }

  build() {
    FakeHubConnection.current = new FakeHubConnection(this.url)
    return FakeHubConnection.current
  }
}

/** What `@microsoft/signalr` is replaced with. */
export const fakeSignalR = {
  HubConnectionBuilder: FakeHubConnectionBuilder,
  HubConnectionState: { Disconnected: 'Disconnected', Connected: 'Connected' },
}
