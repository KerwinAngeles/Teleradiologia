import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { api } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import type { Notificacion, ResumenNotificaciones } from '@/types/notificacion'

const RUTA_HUB = '/hubs/notificaciones'
const MAXIMO_EN_PANEL = 8

export const useNotificacionesStore = defineStore('notificaciones', () => {
  const recientes = ref<Notificacion[]>([])
  const noLeidas = ref(0)
  const conectado = ref(false)

  let conexion: HubConnection | null = null

  const hayNoLeidas = computed(() => noLeidas.value > 0)

  async function cargarResumen() {
    try {
      const { data } = await api.get<ResumenNotificaciones>('/notificaciones/resumen')
      recientes.value = data.recientes
      noLeidas.value = data.noLeidas
    } catch {
      // Sin resumen la campana queda en cero; no vale interrumpir al usuario por esto.
    }
  }

  function recibir(notificacion: Notificacion) {
    recientes.value = [notificacion, ...recientes.value].slice(0, MAXIMO_EN_PANEL)
    noLeidas.value += 1

    const toasts = useToastStore()
    if (notificacion.tipo === 'EstudioUrgente') {
      toasts.error(`${notificacion.titulo} — ${notificacion.mensaje}`)
    } else {
      toasts.info(`${notificacion.titulo} — ${notificacion.mensaje}`)
    }
  }

  async function conectar() {
    const auth = useAuthStore()
    if (!auth.token || conexion) return

    conexion = new HubConnectionBuilder()
      // El token va por accessTokenFactory: el navegador no permite cabeceras en el
      // handshake de WebSocket, así que SignalR lo manda por query string.
      .withUrl(RUTA_HUB, { accessTokenFactory: () => useAuthStore().token ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    conexion.on('notificacion', recibir)
    conexion.onreconnected(() => {
      conectado.value = true
      // Al volver puede haber perdido avisos: se resincroniza con el servidor.
      cargarResumen()
    })
    conexion.onreconnecting(() => (conectado.value = false))
    conexion.onclose(() => (conectado.value = false))

    try {
      await conexion.start()
      conectado.value = true
    } catch {
      conectado.value = false
    }
  }

  async function desconectar() {
    if (conexion && conexion.state !== HubConnectionState.Disconnected) {
      await conexion.stop().catch(() => undefined)
    }
    conexion = null
    conectado.value = false
    recientes.value = []
    noLeidas.value = 0
  }

  async function marcarLeida(id: string) {
    const notificacion = recientes.value.find((n) => n.id === id)
    if (notificacion && !notificacion.leida) {
      noLeidas.value = Math.max(0, noLeidas.value - 1)
      notificacion.leidaAt = new Date().toISOString()
    }

    try {
      await api.post(`/notificaciones/${id}/leida`)
    } catch {
      await cargarResumen()
    }
  }

  async function marcarTodasLeidas() {
    noLeidas.value = 0
    recientes.value = recientes.value.map((n) => ({ ...n, leidaAt: n.leidaAt ?? new Date().toISOString() }))

    try {
      await api.post('/notificaciones/leidas')
    } catch {
      await cargarResumen()
    }
  }

  return {
    recientes,
    noLeidas,
    conectado,
    hayNoLeidas,
    cargarResumen,
    conectar,
    desconectar,
    marcarLeida,
    marcarTodasLeidas,
  }
})
