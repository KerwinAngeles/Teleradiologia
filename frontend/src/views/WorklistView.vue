<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { api } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import { useEstadisticas, type TonoKpi } from '@/composables/useEstadisticas'
import TarjetaGrafico from '@/components/charts/TarjetaGrafico.vue'
import GraficoBarras from '@/components/charts/GraficoBarras.vue'
import GraficoDona from '@/components/charts/GraficoDona.vue'
import GraficoTendencia from '@/components/charts/GraficoTendencia.vue'
import { useReloj, formatearRestante } from '@/composables/useReloj'
import { useDebounce } from '@/composables/useDebounce'
import Paginacion from '@/components/Paginacion.vue'
import type { Estudio, EstudioEstadistica, EstadoEstudio, EstadoSla, PrioridadEstudio } from '@/types/estudio'
import type { PagedResult } from '@/types/pagina'

const { ahora } = useReloj()

const auth = useAuthStore()
const toasts = useToastStore()

const estudios = ref<Estudio[]>([])
const estadisticas = ref<EstudioEstadistica[]>([])
const cargando = ref(true)
const error = ref<string | null>(null)
const tomando = ref<string | null>(null)

const pagina = ref(1)
const tamanoPagina = ref(20)
const total = ref(0)

const fEstado = ref<EstadoEstudio | ''>('')
const fPrioridad = ref<PrioridadEstudio | ''>('')
const fTexto = ref('')
const fVencidos = ref(false)
const fAsignadoAMi = ref(false)

const textoDebounced = useDebounce(fTexto)

const hayFiltros = computed(
  () => !!fEstado.value || !!fPrioridad.value || !!fTexto.value || fVencidos.value || fAsignadoAMi.value,
)

function limpiarFiltros() {
  fEstado.value = ''
  fPrioridad.value = ''
  fTexto.value = ''
  fVencidos.value = false
  fAsignadoAMi.value = false
}

async function cargar() {
  cargando.value = true
  error.value = null
  try {
    const { data } = await api.get<PagedResult<Estudio>>('/estudios', {
      params: {
        pageNumber: pagina.value,
        pageSize: tamanoPagina.value,
        estado: fEstado.value || undefined,
        prioridad: fPrioridad.value || undefined,
        texto: textoDebounced.value.trim() || undefined,
        soloVencidos: fVencidos.value || undefined,
        asignadoAMi: fAsignadoAMi.value || undefined,
      },
    })
    estudios.value = data.items
    total.value = data.totalCount
  } catch {
    error.value = 'No se pudieron cargar los estudios.'
  } finally {
    cargando.value = false
  }
}

// Los KPIs y gráficos van sobre el total, no sobre la página: se piden aparte.
async function cargarEstadisticas() {
  try {
    const { data } = await api.get<EstudioEstadistica[]>('/estudios/estadisticas')
    estadisticas.value = data
  } catch {
    estadisticas.value = []
  }
}

// Cualquier filtro nuevo vuelve a la primera página: si no, se cae en una vacía.
watch([fEstado, fPrioridad, textoDebounced, fVencidos, fAsignadoAMi], () => {
  pagina.value = 1
  cargar()
})

watch(pagina, cargar)

onMounted(() => {
  cargar()
  cargarEstadisticas()
})

const esRadiologo = computed(() => auth.usuario?.rol === 'Radiologo')

const usuario = computed(() => auth.usuario)
const {
  kpis,
  barras,
  panelBarras,
  leyendaBarras,
  dona,
  panelDona,
  puntosTendencia,
  seriesTendencia,
  panelTendencia,
  balanceTendencia,
} = useEstadisticas(estadisticas, usuario)

// El resplandor de cada KPI codifica su estado, no su posición en la grilla.
const brilloPorTono: Record<TonoKpi, string> = {
  neutro: 'var(--color-viz-serie)',
  atencion: 'var(--color-viz-pendiente)',
  alerta: 'var(--color-estado-stat)',
  bien: 'var(--color-viz-informado)',
}

const totalDona = computed(() => dona.value.reduce((s, d) => s + d.valor, 0))

const notaTendencia = computed(() => {
  const { entradas, salidas, neto } = balanceTendencia.value
  if (entradas === 0 && salidas === 0) return undefined
  if (neto > 0) return `Entraron ${neto} más de los que salieron: la cola creció en estas dos semanas.`
  if (neto < 0) return `Salieron ${Math.abs(neto)} más de los que entraron: la cola se descargó.`
  return 'Entró y salió lo mismo: la cola quedó estable.'
})

async function tomar(id: string) {
  const estudio = estudios.value.find((e) => e.id === id)
  tomando.value = id
  error.value = null
  try {
    await api.post(`/estudios/${id}/tomar`)
    await Promise.all([cargar(), cargarEstadisticas()])
    toasts.exito(`Tomaste el estudio de ${estudio?.pacienteNombre ?? 'paciente'}. Ya podés informarlo.`)
  } catch {
    const mensaje = 'No se pudo tomar el estudio — puede que otro radiólogo ya lo haya tomado.'
    error.value = mensaje
    toasts.error(mensaje)
  } finally {
    tomando.value = null
  }
}

const prioridadChip: Record<PrioridadEstudio, string> = {
  Stat: 'chip-stat',
  Urgente: 'chip-urgente',
  Rutina: 'chip-neutro',
}
const prioridadLabel: Record<PrioridadEstudio, string> = {
  Stat: 'STAT',
  Urgente: 'Urgente',
  Rutina: 'Rutina',
}

const slaChip: Record<EstadoSla, string> = {
  EnPlazo: 'chip-informado',
  PorVencer: 'chip-pendiente',
  Vencido: 'chip-vencido',
  Cumplido: 'chip-informado',
  Incumplido: 'chip-vencido',
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

const formatoFecha = new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
</script>

<template>
  <div class="stagger space-y-7">
    <div>
      <p class="meta-label">Cola de lectura</p>
      <h1 class="display mt-1.5 text-3xl sm:text-4xl">Worklist</h1>
    </div>

    <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
      <div v-for="kpi in kpis" :key="kpi.etiqueta" class="glass relative overflow-hidden p-5">
        <div
          aria-hidden="true"
          class="absolute inset-x-0 bottom-0 h-24"
          :style="{
            background: `linear-gradient(to top, color-mix(in srgb, ${brilloPorTono[kpi.tono]} 26%, transparent), transparent)`,
          }"
        />
        <div class="relative">
          <p class="meta-label">{{ kpi.etiqueta }}</p>
          <p class="mt-2 text-4xl font-light tabular-nums">{{ kpi.valor }}</p>
          <p class="text-ink-faint mt-1 text-xs">{{ kpi.detalle }}</p>
        </div>
      </div>
    </div>

    <div class="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <TarjetaGrafico
        :titulo="panelBarras.titulo"
        :subtitulo="panelBarras.subtitulo"
        :leyenda="leyendaBarras"
      >
        <GraficoBarras :datos="barras" />

        <template #tabla>
          <table class="w-full">
            <thead>
              <tr class="border-b border-[var(--color-hairline)]">
                <th class="meta-label py-1.5 text-left font-semibold">Categoría</th>
                <th
                  v-for="serie in leyendaBarras"
                  :key="serie.clave"
                  class="meta-label py-1.5 text-right font-semibold"
                >
                  {{ serie.etiqueta }}
                </th>
                <th class="meta-label py-1.5 text-right font-semibold">Total</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="fila in barras"
                :key="fila.etiqueta"
                class="border-b border-[var(--color-hairline)] last:border-0"
              >
                <td class="py-1.5 text-xs">{{ fila.etiqueta }}</td>
                <td
                  v-for="segmento in fila.segmentos"
                  :key="segmento.clave"
                  class="text-ink-soft py-1.5 text-right text-xs tabular-nums"
                >
                  {{ segmento.valor }}
                </td>
                <td class="py-1.5 text-right text-xs font-medium tabular-nums">{{ fila.total }}</td>
              </tr>
            </tbody>
          </table>
        </template>
      </TarjetaGrafico>

      <TarjetaGrafico :titulo="panelDona.titulo" :subtitulo="panelDona.subtitulo">
        <GraficoDona :datos="dona" />

        <template #tabla>
          <table class="w-full">
            <thead>
              <tr class="border-b border-[var(--color-hairline)]">
                <th class="meta-label py-1.5 text-left font-semibold">Prioridad</th>
                <th class="meta-label py-1.5 text-right font-semibold">Estudios</th>
                <th class="meta-label py-1.5 text-right font-semibold">%</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="fila in dona"
                :key="fila.clave"
                class="border-b border-[var(--color-hairline)] last:border-0"
              >
                <td class="py-1.5 text-xs">{{ fila.etiqueta }}</td>
                <td class="py-1.5 text-right text-xs tabular-nums">{{ fila.valor }}</td>
                <td class="text-ink-faint py-1.5 text-right text-xs tabular-nums">
                  {{ totalDona === 0 ? '0%' : `${Math.round((fila.valor / totalDona) * 100)}%` }}
                </td>
              </tr>
            </tbody>
          </table>
        </template>
      </TarjetaGrafico>
    </div>

    <TarjetaGrafico
      :titulo="panelTendencia.titulo"
      :subtitulo="panelTendencia.subtitulo"
      :leyenda="seriesTendencia"
      :nota="notaTendencia"
    >
      <GraficoTendencia :datos="puntosTendencia" :series="seriesTendencia" />

      <template #tabla>
        <table class="w-full">
          <thead>
            <tr class="border-b border-[var(--color-hairline)]">
              <th class="meta-label py-1.5 text-left font-semibold">Día</th>
              <th class="meta-label py-1.5 text-right font-semibold">{{ seriesTendencia[0].etiqueta }}</th>
              <th class="meta-label py-1.5 text-right font-semibold">{{ seriesTendencia[1].etiqueta }}</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="fila in puntosTendencia"
              :key="fila.fecha"
              class="border-b border-[var(--color-hairline)] last:border-0"
            >
              <td class="py-1.5 text-xs tabular-nums">{{ fila.etiqueta }}</td>
              <td class="py-1.5 text-right text-xs tabular-nums">{{ fila.entradas }}</td>
              <td class="py-1.5 text-right text-xs tabular-nums">{{ fila.salidas }}</td>
            </tr>
          </tbody>
        </table>
      </template>
    </TarjetaGrafico>

    <div class="glass flex flex-wrap items-center gap-3 p-4">
      <div class="relative min-w-[220px] flex-1">
        <svg
          class="text-ink-faint pointer-events-none absolute top-1/2 left-3.5 h-4 w-4 -translate-y-1/2"
          fill="none"
          viewBox="0 0 24 24"
          stroke-width="1.6"
          stroke="currentColor"
        >
          <path stroke-linecap="round" stroke-linejoin="round" d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z" />
        </svg>
        <input v-model="fTexto" type="search" placeholder="Paciente o documento…" class="field !pl-10" />
      </div>

      <select v-model="fEstado" class="field !w-auto">
        <option value="">Todos los estados</option>
        <option value="Pendiente">Pendiente</option>
        <option value="EnInforme">En informe</option>
        <option value="Informado">Informado</option>
      </select>

      <select v-model="fPrioridad" class="field !w-auto">
        <option value="">Toda prioridad</option>
        <option value="Stat">STAT</option>
        <option value="Urgente">Urgente</option>
        <option value="Rutina">Rutina</option>
      </select>

      <button
        type="button"
        class="chip"
        :class="fVencidos ? 'chip-vencido' : 'chip-neutro'"
        @click="fVencidos = !fVencidos"
      >
        Fuera de plazo
      </button>

      <button
        v-if="esRadiologo"
        type="button"
        class="chip"
        :class="fAsignadoAMi ? 'chip-informe' : 'chip-neutro'"
        @click="fAsignadoAMi = !fAsignadoAMi"
      >
        Asignados a mí
      </button>

      <button v-if="hayFiltros" type="button" class="btn-ghost !px-3.5 !py-1.5 !text-xs" @click="limpiarFiltros">
        Limpiar
      </button>
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
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Prioridad</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Plazo</th>
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
              <td class="text-ink-soft px-5 py-3.5 text-sm">{{ estudio.hospitalNombre }}</td>
              <td class="text-ink-soft px-5 py-3.5 text-sm tabular-nums">
                {{ formatoFecha.format(new Date(estudio.fechaEstudio)) }}
              </td>
              <td class="px-5 py-3.5">
                <span class="chip" :class="prioridadChip[estudio.prioridad]">
                  {{ prioridadLabel[estudio.prioridad] }}
                </span>
              </td>
              <td class="px-5 py-3.5">
                <span class="chip" :class="slaChip[estudio.estadoSla]">
                  {{ estudio.estadoSla === 'Cumplido' ? 'A tiempo' : estudio.estadoSla === 'Incumplido' ? 'Fuera de plazo' : formatearRestante(estudio.fechaLimite, ahora) }}
                </span>
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
              <td colspan="9" class="text-ink-faint px-5 py-12 text-center text-sm">Cargando estudios…</td>
            </tr>
            <tr v-else-if="estudios.length === 0">
              <td colspan="9" class="text-ink-faint px-5 py-12 text-center text-sm">{{ hayFiltros ? "Ningún estudio coincide con los filtros." : "No hay estudios todavía." }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <Paginacion
        :pagina="pagina"
        :tamano-pagina="tamanoPagina"
        :total="total"
        @cambiar="(p) => (pagina = p)"
      />
    </div>

    <p v-if="error" class="text-sm text-red-700">{{ error }}</p>
  </div>
</template>
