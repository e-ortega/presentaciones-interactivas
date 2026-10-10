/** A response the server answered with an error status. */
export class ApiError extends Error {
  status: number
  /** Machine-readable reason sent by the server, e.g. "NotPptx". */
  code?: string
  /** The limit that was exceeded, when the reason is one: megabytes or slides. */
  limit?: number

  constructor(status: number, code?: string, limit?: number) {
    super(`Request failed with status ${status}`)
    this.status = status
    this.code = code
    this.limit = limit
  }
}

/** Returns the response when it is a success; throws an ApiError otherwise. */
export async function ensureOk(response: Response): Promise<Response> {
  if (response.ok) {
    return response
  }

  let code: string | undefined
  let limit: number | undefined
  try {
    const problem: unknown = await response.json()
    if (typeof problem === 'object' && problem !== null) {
      if ('code' in problem && typeof problem.code === 'string') {
        code = problem.code
      }
      if ('limit' in problem && typeof problem.limit === 'number') {
        limit = problem.limit
      }
    }
  } catch {
    // Not every error has a JSON body (a 429, for example).
  }

  throw new ApiError(response.status, code, limit)
}
