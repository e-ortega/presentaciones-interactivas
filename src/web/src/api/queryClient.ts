import { MutationCache, QueryCache, QueryClient, type QueryClientConfig } from '@tanstack/react-query'
import { ApiError } from './http'

/** Query key of "does this browser hold the teacher cookie?" (see AccessGate). */
export const accessQueryKey = ['access']

/**
 * The app's query client. Any request answered with 401 means the teacher cookie expired
 * or was revoked, so access is marked as lost and AccessGate asks for the key again,
 * instead of each screen showing its own "could not load" error.
 */
export function createQueryClient(defaultOptions?: QueryClientConfig['defaultOptions']): QueryClient {
  const forgetAccessOn401 = (error: Error) => {
    if (error instanceof ApiError && error.status === 401) {
      queryClient.setQueryData(accessQueryKey, false)
    }
  }

  const queryClient = new QueryClient({
    defaultOptions,
    queryCache: new QueryCache({ onError: forgetAccessOn401 }),
    mutationCache: new MutationCache({ onError: forgetAccessOn401 }),
  })
  return queryClient
}
