<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { api } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import { useEstadisticas } from '@/composables/useEstadisticas'
import TarjetaGrafico from '@/components/charts/TarjetaGrafico.vue'
import GraficoBarras from '@/components/charts/GraficoBarras.vue'
import GraficoDona from '@/components/charts/GraficoDona.vue'
import type { Estudio } from '@/types/estudio'

const auth = useAuthStore()
const toasts = useToastStore()

const estudios = ref<Estudio[]>([])
const cargando = ref(true)
const error = ref<string | null>(null)
const tomando = ref<string | null>(null)

async function cargar() {
  cargando.value = true
  error.value = null
  try {
    const { data } = await api.get<Estudio[]>('/estudios')
    estudios.value = data
  } catch {
    error.value = 'No se pudieron cargar los estudios.'
  } finally {
    cargando.value = false
  }
}

onMounted(cargar)

const esRadiologo = computed(() => auth.usuario?.rol === 'Radiologo')

const usuario = computed(() => auth.usuario)
const { kpis, dona, panelDona, barras, panelBarras } = useEstadisticas(estudios, usuario)

async function tomar(id: string) {
  const estudio = estudios.value.find((e) => e.id === id)
  tomando.value = id
  error.value = null
  try {
    await api.post(`/estudios/${id}/tomar`)
    await cargar()
    toasts.exito(`Tomaste el estudio de ${estudio?.pacienteNombre ?? 'paciente'}. Ya podés informarlo.`)
  } catch {
    const mensaje = 'No se pudo tomar el estudio — puede que otro radiólogo ya lo haya tomado.'
    error.value = mensaje
    toasts.error(mensaje)
  } finally {
    tomando.value = null
  }
}

const estadoChip: Record<Estudio['estado'], string> = {
  Pendiente: 'chip-pendiente',
  EnInforme: 'chip-informe',
  Informado: 'chip-informado',
}
const estadoLabel: Record<Estudio['estado'], string> = {
  Pendiente: 'Pendiente',
  EnInforme: 'En informe',
  Informado: 'Informado',
}

const brillos = ['from-coral/70', 'from-lilac/70', 'from-aqua/70', 'from-coral/50']

const formatoFecha = new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
</script>

<template>
  <div class="stagger space-y-7">
    <div class="flex flex-wrap items-end justify-between gap-4">
      <div>
        <p class="meta-label">Cola de lectura</p>
        <h1 class="display mt-1.5 text-3xl sm:text-4xl">Worklist</h1>
      </div>
      <p class="text-ink-soft text-sm">
        {{ estudios.length }} estudio{{ estudios.length === 1 ? '' : 's' }} en la plataforma
      </p>
    </div>

    <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
      <div v-for="(kpi, i) in kpis" :key="kpi.etiqueta" class="glass relative overflow-hidden p-5">
        <div
          class="absolute inset-x-0 bottom-0 h-24 bg-gradient-to-t to-transparent opacity-60"
          :class="brillos[i % brillos.length]"
        />
        <div class="relative">
          <p class="meta-label">{{ kpi.etiqueta }}</p>
          <p class="mt-2 text-4xl font-light">{{ kpi.valor }}</p>
          <p class="text-ink-faint mt-1 text-xs">{{ kpi.detalle }}</p>
        </div>
      </div>
    </div>

    <div class="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <TarjetaGrafico :titulo="panelBarras.titulo" :subtitulo="panelBarras.subtitulo" :datos="barras">
        <GraficoBarras :datos="barras" />
      </TarjetaGrafico>

      <TarjetaGrafico :titulo="panelDona.titulo" :subtitulo="panelDona.subtitulo" :datos="dona">
        <GraficoDona :datos="dona" />
      </TarjetaGrafico>
    </div>

    <div class="glass overflow-hidden">
      <div class="overflow-x-auto">
        <table class="min-w-full">
          <thead>
            <tr class="border-b border-[var(--color-hairline)]">
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Paciente</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Modalidad</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Hospital</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Fecha</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Estado</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Radiólogo</th>
              <th class="px-5 py-3.5"></th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="estudio in estudios"
              :key="estudio.id"
              class="border-b border-[var(--color-hairline)] transition-colors last:border-0 hover:bg-white/55"
            >
              <td class="px-5 py-3.5">
                <p class="text-sm font-medium">{{ estudio.pacienteNombre }}</p>
                <p class="text-ink-faint text-xs">{{ estudio.pacienteDocumento }}</p>
              </td>
              <td class="text-ink-soft px-5 py-3.5 text-sm">{{ estudio.modalidad }}</td>
              <td class="text-ink-soft px-5 py-3.5 text-sm">{{ estudio.hospitalOrigen }}</td>
              <td class="text-ink-soft px-5 py-3.5 text-sm tabular-nums">
                {{ formatoFecha.format(new Date(estudio.fechaEstudio)) }}
              </td>
              <td class="px-5 py-3.5">
                <span class="chip" :class="estadoChip[estudio.estado]">{{ estadoLabel[estudio.estado] }}</span>
              </td>
              <td class="text-ink-soft px-5 py-3.5 text-sm">{{ estudio.radiologoAsignadoNombre ?? '—' }}</td>
              <td class="px-5 py-3.5">
                <div class="flex items-center justify-end gap-2">
                  <button
                    v-if="esRadiologo && estudio.estado === 'Pendiente'"
                    type="button"
                    :disabled="tomando === estudio.id"
                    class="btn-ink !px-3.5 !py-1.5 !text-xs"
                    @click="tomar(estudio.id)"
                  >
                    {{ tomando === estudio.id ? 'Tomando…' : 'Tomar' }}
                  </button>
                  <RouterLink :to="`/estudios/${estudio.id}`" class="btn-orb" title="Abrir estudio">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.6" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6m0 0H9m9 0v9" />
                    </svg>
                  </RouterLink>
                </div>
              </td>
            </tr>
            <tr v-if="cargando">
              <td colspan="7" class="text-ink-faint px-5 py-12 text-center text-sm">Cargando estudios…</td>
            </tr>
            <tr v-else-if="estudios.length === 0">
              <td colspan="7" class="text-ink-faint px-5 py-12 text-center text-sm">No hay estudios todavía.</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <p v-if="error" class="text-sm text-red-700">{{ error }}</p>
  </div>
</template>
