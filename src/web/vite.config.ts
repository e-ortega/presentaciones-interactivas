import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// In development the browser talks only to Vite, which forwards API calls to
// ASP.NET Core. Same origin, as in production: no CORS and cookies just work.
const server = 'http://localhost:5049'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': server,
      '/health': server,
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
  },
})
