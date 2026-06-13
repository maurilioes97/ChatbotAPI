import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    host: true, // ESSENCIAL para o Docker
    watch: {
      usePolling: true, // Ajuda a atualizar o código automaticamente no Windows
    },
    open: false // Recomendo deixar false no Docker para não tentar abrir o browser dentro do container
  }
})
