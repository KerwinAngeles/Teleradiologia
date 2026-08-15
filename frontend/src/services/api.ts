import axios from 'axios'
import router from '@/router'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'

export const api = axios.create({
  baseURL: '/api',
})

api.interceptors.request.use((config) => {
  const auth = useAuthStore()
  if (auth.token) {
    config.headers.Authorization = `Bearer ${auth.token}`
  }
  return config
})

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      const auth = useAuthStore()
      const teniaSesion = auth.estaAutenticado
      auth.logout()
      if (router.currentRoute.value.name !== 'login') {
        router.push({ name: 'login', query: { redirect: router.currentRoute.value.fullPath } })
        // Sin el aviso, una sesión vencida parece una expulsión sin motivo.
        if (teniaSesion) {
          useToastStore().error('Tu sesión expiró. Volvé a entrar para continuar.')
        }
      }
    }
    return Promise.reject(error)
  },
)
