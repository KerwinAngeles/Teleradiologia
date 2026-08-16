<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { isAxiosError } from 'axios'
import { api } from '@/services/api'
import { useToastStore } from '@/stores/toast'

interface FirmasPorRadiologo {
  radiologo: string
  firmados: number
}

interface ResumenActividad {
  desde: string
  hasta: string
  estudiosRecibidos: number
  informesFirmados: number
  adendasFirmadas: number
  estudiosInformados: number
  estudiosPendientes: number
  estudiosEnInforme: number
  porRadiologo: FirmasPorRadiologo[]
  sinActividad: boolean
}

const toasts = useToastStore()

const resumen = ref<ResumenActividad | null>(null)
const dias = ref(7)
const cargando = ref(true)
const enviando = ref(false)
const error = ref<string | null>(null)

async function cargar() {
  cargando.value = true
  error.value = null
  try {
    const { data } = await api.get<ResumenActividad>('/resumen', { params: { dias: dias.value } })
    resumen.value = data
  } catch {
    error.value = 'No se pudo cargar el resumen.'
  } finally {
    cargando.value = false
  }
}

onMounted(cargar)

async function enviar() {
  enviando.value = true
  try {
    const { data } = await api.post<number>('/resumen/enviar', null, { params: { dias: dias.value } })
    toasts.exito(
      data > 0
        ? `Resumen enviado a ${data} administrador${data === 1 ? '' : 'es'}.`
        : 'Sin actividad en el período: no se envió nada.',
    )
  } catch (e) {
    const mensaje: string =
      isAxiosError(e) && e.response?.data?.detail ? e.response.data.detail : 'No se pudo enviar el resumen.'
    toasts.error(mensaje)
  } finally {
    enviando.value = false
  }
}

const periodos = [1, 7, 30, 90]

const formatoFecha = new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
</script>

<template>
  <div class="stagger space-y-7">
    <div class="flex flex-wrap items-end justify-between gap-4">
      <div>
        <RouterLink to="/configuracion" class="meta-label hover:text-ink transition-colors">
          ← Configuración
        </RouterLink>
        <h1 class="display mt-1.5 text-3xl sm:text-4xl">Resumen de actividad</h1>
        <p class="text-ink-soft mt-2 text-sm">
          El worker lo envía por email a los administradores todos los días. Acá podés consultarlo o forzar un envío.
        </p>
      </div>
      <button type="button" :disabled="enviando || cargando" class="btn-ink" @click="enviar">
        {{ enviando ? 'Enviando…' : 'Enviar ahora' }}
      </button>
    </div>

    <div class="flex flex-wrap gap-2">
      <button
        v-for="p in periodos"
        :key="p"
        type="button"
        class="chip"
        :class="dias === p ? 'chip-informe' : 'chip-neutro'"
        @click="((dias = p), cargar())"
      >
        {{ p === 1 ? 'Último día' : `Últimos ${p} días` }}
      </button>
    </div>

    <p v-if="cargando" class="text-ink-faint py-12 text-center text-sm">Cargando resumen…</p>
    <p v-else-if="error" class="text-sm text-red-700">{{ error }}</p>

    <template v-else-if="resumen">
      <div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <div class="glass p-5">
          <p class="meta-label">Estudios recibidos</p>
          <p class="mt-2 text-4xl font-light tabular-nums">{{ resumen.estudiosRecibidos }}</p>
        </div>
        <div class="glass p-5">
          <p class="meta-label">Informes firmados</p>
          <p class="mt-2 text-4xl font-light tabular-nums">{{ resumen.informesFirmados }}</p>
        </div>
        <div class="glass p-5">
          <p class="meta-label">Adendas firmadas</p>
          <p class="mt-2 text-4xl font-light tabular-nums">{{ resumen.adendasFirmadas }}</p>
        </div>
      </div>

      <div class="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <div class="glass p-6">
          <p class="meta-label">Estado de la cola</p>
          <div class="mt-4">
            <div class="meta-row">
              <span class="text-sm">Pendientes</span>
              <span class="text-sm font-medium tabular-nums">{{ resumen.estudiosPendientes }}</span>
            </div>
            <div class="meta-row">
              <span class="text-sm">En informe</span>
              <span class="text-sm font-medium tabular-nums">{{ resumen.estudiosEnInforme }}</span>
            </div>
            <div class="meta-row">
              <span class="text-sm">Informados</span>
              <span class="text-sm font-medium tabular-nums">{{ resumen.estudiosInformados }}</span>
            </div>
          </div>
        </div>

        <div class="glass p-6">
          <p class="meta-label">Firmas por radiólogo</p>
          <div v-if="resumen.porRadiologo.length > 0" class="mt-4">
            <div v-for="fila in resumen.porRadiologo" :key="fila.radiologo" class="meta-row">
              <span class="text-sm">{{ fila.radiologo }}</span>
              <span class="text-sm font-medium tabular-nums">{{ fila.firmados }}</span>
            </div>
          </div>
          <p v-else class="text-ink-faint mt-4 text-sm">Nadie firmó informes en este período.</p>
        </div>
      </div>

      <p class="text-ink-faint text-xs">
        Período: {{ formatoFecha.format(new Date(resumen.desde)) }} — {{ formatoFecha.format(new Date(resumen.hasta)) }}
      </p>
    </template>
  </div>
</template>
