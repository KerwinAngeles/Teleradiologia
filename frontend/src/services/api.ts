import axios from 'axios'

/**
 * Instancia de Axios preconfigurada. En dev, Vite hace proxy de /api hacia el
 * backend (ver vite.config.ts); en producción se sirve tras el mismo dominio/gateway.
 */
export const api = axios.create({
  baseURL: '/api',
})
