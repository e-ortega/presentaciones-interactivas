/** Asks the server whether it is alive. Resolves on 200, throws otherwise. */
export async function fetchHealth(): Promise<string> {
  const response = await fetch('/health')
  if (!response.ok) {
    throw new Error(`Health check failed with status ${response.status}`)
  }
  return response.text()
}
