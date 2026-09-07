import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// Dev proxy lets `npm run dev` work with zero CORS friction:
// fetch('/api/links') -> http://api.localhost/api/links (see compose.yaml Traefik route).
// For a split-origin deployment, set VITE_API_BASE_URL and rely on the API's CORS policy instead.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: process.env.VITE_API_PROXY_TARGET ?? 'http://api.localhost',
        changeOrigin: true,
      },
      '/healthz': {
        target: process.env.VITE_API_PROXY_TARGET ?? 'http://api.localhost',
        changeOrigin: true,
      },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: false,
  },
})
