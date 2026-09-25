import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig(({ mode }) => {
  const proxy = loadEnv(mode, '.', '').API_PROXY || 'http://localhost:5000'
  return {
  plugins: [react(), tailwindcss()],
  server: { proxy: {
    '/api': proxy,
    '/chatHub': { target: proxy, ws: true },
  } },
}})
