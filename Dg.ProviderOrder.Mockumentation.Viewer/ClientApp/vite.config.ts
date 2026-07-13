import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Builds into the host's wwwroot so ASP.NET Core serves the SPA. During `npm run dev`,
// /api is proxied to the running .NET host.
export default defineConfig({
  plugins: [react()],
  base: './',
  build: {
    outDir: '../wwwroot',
    emptyOutDir: true,
  },
  server: {
    proxy: {
      '/api': 'http://localhost:5240',
    },
  },
});
