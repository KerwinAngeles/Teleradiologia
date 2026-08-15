import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig({
  plugins: [vue(), tailwindcss()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
      // vtk.js arrastra xmlbuilder2, que extiende el EventEmitter de Node.
      // Sin esto: "Class extends value undefined".
      events: 'events',
    },
  },
  optimizeDeps: {
    // Trae web workers y WASM que esbuild rompe al empaquetar.
    exclude: ['@cornerstonejs/dicom-image-loader'],
    // CommonJS que solo alcanza el loader excluido, así que Vite no los descubre solo.
    // Van los subpaths exactos que importan los decoders: listar la raíz no surte efecto.
    include: [
      'dicom-parser',
      '@cornerstonejs/codec-charls/decodewasmjs',
      '@cornerstonejs/codec-libjpeg-turbo-8bit/decodewasmjs',
      '@cornerstonejs/codec-openjpeg/decodewasmjs',
      '@cornerstonejs/codec-openjph/wasmjs',
    ],
  },
  worker: {
    format: 'es',
  },
  server: {
    proxy: {
      // 127.0.0.1 y no localhost: por ::1 el puerto que publica Docker no responde
      // y el proxy se cuelga esperando en vez de fallar.
      '/api': {
        target: 'http://127.0.0.1:5080',
        changeOrigin: true,
      },
    },
  },
  // `vite preview` no hereda el proxy de `server`.
  preview: {
    proxy: {
      '/api': {
        target: 'http://127.0.0.1:5080',
        changeOrigin: true,
      },
    },
  },
})
