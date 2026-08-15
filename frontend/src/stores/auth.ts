import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { api } from '@/services/api'
import type { LoginResponse, RegistroResponse, Usuario } from '@/types/auth'

const STORAGE_KEY = 'teleradiologia.auth'

interface SesionGuardada {
  token: string
  refreshToken: string | null
  usuario: Usuario
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(null)
  const refreshToken = ref<string | null>(null)
  const usuario = ref<Usuario | null>(null)

  // Hidratar desde localStorage: un refresh no desloguea.
  const guardada = localStorage.getItem(STORAGE_KEY)
  if (guardada) {
    try {
      const sesion = JSON.parse(guardada) as SesionGuardada
      token.value = sesion.token
      refreshToken.value = sesion.refreshToken ?? null
      usuario.value = sesion.usuario
    } catch {
      localStorage.removeItem(STORAGE_KEY)
    }
  }

  const estaAutenticado = computed(() => token.value !== null)
  const esAdmin = computed(() => usuario.value?.rol === 'Admin')

  function persistir() {
    if (token.value && usuario.value) {
      const sesion: SesionGuardada = {
        token: token.value,
        refreshToken: refreshToken.value,
        usuario: usuario.value,
      }
      localStorage.setItem(STORAGE_KEY, JSON.stringify(sesion))
    } else {
      localStorage.removeItem(STORAGE_KEY)
    }
  }

  async function login(email: string, password: string) {
    const { data } = await api.post<LoginResponse>('/account/login', { email, password })
    token.value = data.token
    refreshToken.value = data.refreshToken
    usuario.value = data.usuario
    persistir()
  }

  async function registrar(nombreCompleto: string, email: string, password: string) {
    const { data } = await api.post<RegistroResponse>('/account/registro', {
      nombreCompleto,
      email,
      password,
    })
    return data
  }

  function logout() {
    token.value = null
    refreshToken.value = null
    usuario.value = null
    persistir()
  }

  return { token, refreshToken, usuario, estaAutenticado, esAdmin, login, registrar, logout }
})
