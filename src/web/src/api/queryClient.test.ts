import { expect, test } from 'vitest'
import { ApiError } from './http'
import { createQueryClient, shouldRetry } from './queryClient'

test('a network failure or a server error is retried up to three times', () => {
  expect(shouldRetry(0, new TypeError('Failed to fetch'))).toBe(true)
  expect(shouldRetry(2, new ApiError(503))).toBe(true)
  expect(shouldRetry(3, new ApiError(503))).toBe(false)
})

test('an answer the server will not change is not retried', () => {
  expect(shouldRetry(0, new ApiError(401))).toBe(false)
  expect(shouldRetry(0, new ApiError(404))).toBe(false)
  expect(shouldRetry(0, new ApiError(400, 'NotPptx'))).toBe(false)
})

test('the app client uses that rule unless a test overrides it', () => {
  expect(createQueryClient().getDefaultOptions().queries?.retry).toBe(shouldRetry)
  expect(createQueryClient({ queries: { retry: false } }).getDefaultOptions().queries?.retry).toBe(false)
})
