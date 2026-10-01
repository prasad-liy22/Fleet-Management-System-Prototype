import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: { proxy: { '/api': 'http://localhost:5080', '/health': 'http://localhost:5080' } },
  test: { maxWorkers: 1, testTimeout: 15000, environment: 'jsdom', setupFiles: ['./src/test/setup.ts'], css: true },
});
