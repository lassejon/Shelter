import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import path from 'path';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 3000,
    strictPort: true,
  },
  build: {
    // The `maps` chunk is unavoidably ~700 kB (deck.gl + Google Maps + supercluster);
    // raise the warning floor so it isn't a wolf-cry on every build.
    chunkSizeWarningLimit: 800,
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (!id.includes('/node_modules/')) return;
          if (
            id.includes('/deck.gl/') ||
            id.includes('@deck.gl/') ||
            id.includes('@vis.gl/react-google-maps') ||
            id.includes('/supercluster/')
          ) {
            return 'maps';
          }
          if (
            id.includes('/react-hook-form/') ||
            id.includes('/zod/') ||
            id.includes('@hookform/') ||
            id.includes('/react-day-picker/') ||
            id.includes('/date-fns/')
          ) {
            return 'forms';
          }
          if (id.includes('@radix-ui/')) {
            return 'radix';
          }
          if (id.includes('/lucide-react/')) {
            return 'lucide';
          }
          if (
            id.includes('/react/') ||
            id.includes('/react-dom/') ||
            id.includes('/react-router/') ||
            id.includes('/scheduler/')
          ) {
            return 'vendor';
          }
        },
      },
    },
  },
});
