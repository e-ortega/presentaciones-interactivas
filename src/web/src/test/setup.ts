import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach, vi } from 'vitest'
import '../i18n'

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  // A spy or fake timer left by one test must not change how the next one behaves.
  vi.restoreAllMocks()
  vi.useRealTimers()
})
