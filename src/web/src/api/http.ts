/** A response the server answered with an error status. */
export class ApiError extends Error {
  status: number
  /** Machine-readable reason sent by the server, e.g. "NotPptx". */
  code?: string

  constructor(status: number, code?: string) {
    super(`Request failed with status ${status}`)
    this.status = status
    this.code = code
  }
}

/** Returns the response when it is a success; throws an ApiError otherwise. */
export async function ensureOk(response: Response): Promise<Response> {
  if (response.ok) {
    return response
  }

  let code: string | undefined
  try {
    const problem: unknown = await response.json()
    if (typeof problem === 'object' && problem !== null && 'code' in problem && typeof problem.code === 'string') {
      code = problem.code
    }
  } catch {
    // Not every error has a JSON body (a 429, for example).
  }

  throw new ApiError(response.status, code)
}
