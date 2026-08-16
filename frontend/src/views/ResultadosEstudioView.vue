<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { api } from '@/services/api'
import { useDebounce } from '@/composables/useDebounce'
import Paginacion from '@/components/Paginacion.vue'
import Modal from '@/components/Modal.vue'
import FormularioResultadoEstudio from '@/components/FormularioResultadoEstudio.vue'
import type { PagedResult } from '@/types/pagina'
import type { Estudio, EstadoEstudio, PrioridadEstudio } from '@/types/estudio'

const estudios = ref<Estudio[]>([])
const cargando = ref(true)
const error = ref<string | null>(null)

const fEstado = ref<EstadoEstudio | ''>('')
const fPrioridad = ref<PrioridadEstudio | ''>('')
const fTexto = ref('')
const textoDebounced = useDebounce(fTexto)

const pagina = ref(1)
const tamanoPagina = ref(20)
const total = ref(0)

const modalAbierto = ref(false)

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

watch([fEstado, fPrioridad, textoDebounced], () => {
  pagina.value = 1
  cargar()
})

watch(pagina, cargar)

onMounted(cargar)

const hayFiltros = computed(() => Boolean(fEstado.value || fPrioridad.value || fTexto.value.trim()))

async function alSubir() {
  modalAbierto.value = false
  pagina.value = 1
  await cargar()
}

const estadoChip: Record<EstadoEstudio, string> = {
  Pendiente: 'chip-pendiente',
  EnInforme: 'chip-informe',
  Informado: 'chip-informado',
}

const estadoLabel: Record<EstadoEstudio, string> = {
  Pendiente: 'Pendiente',
  EnInforme: 'En informe',
  Informado: 'Informado',
}

const prioridadChip: Record<PrioridadEstudio, string> = {
  Rutina: 'chip-neutro',
  Urgente: 'chip-urgente',
  Stat: 'chip-stat',
}

const prioridadLabel: Record<PrioridadEstudio, string> = {
  Rutina: 'Rutina',
  Urgente: 'Urgente',
  Stat: 'STAT',
}

const formatoFecha = new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
</script>

<template>
  <div class="stagger space-y-7">
    <div class="flex flex-wrap items-end justify-between gap-4">
      <div>
        <p class="meta-label">Ingreso de estudios</p>
        <h1 class="display mt-1.5 text-3xl sm:text-4xl">Resultados estudio</h1>
        <p class="text-ink-soft mt-2 max-w-xl text-sm leading-relaxed">
          Estudios cargados en la plataforma. La metadata del paciente sale de los tags DICOM — no hay que tipearla.
        </p>
      </div>
      <button type="button" class="btn-ink" @click="modalAbierto = true">
        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
        </svg>
        Agregar resultado estudio
      </button>
    </div>

    <div class="glass flex flex-wrap items-center gap-3 p-4">
      <input v-model="fTexto" type="search" placeholder="Paciente, documento o modalidad…" class="field min-w-[220px] flex-1" />
      <select v-model="fEstado" class="field !w-auto">
        <option value="">Todos los estados</option>
        <option value="Pendiente">Pendiente</option>
        <option value="EnInforme">En informe</option>
        <option value="Informado">Informado</option>
      </select>
      <select v-model="fPrioridad" class="field !w-auto">
        <option value="">Todas las prioridades</option>
        <option value="Stat">STAT</option>
        <option value="Urgente">Urgente</option>
        <option value="Rutina">Rutina</option>
      </select>
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
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Estado</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Radiólogo</th>
              <th class="px-5 py-3.5"></th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="estudio in estudios"
              :key="estudio.id"
              class="border-b border-[var(--color-hairline)] transition-colors last:border-0 hover:bg-[var(--color-superficie-suave)]"
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
                <span class="chip" :class="estadoChip[estudio.estado]">{{ estadoLabel[estudio.estado] }}</span>
              </td>
              <td class="text-ink-soft px-5 py-3.5 text-sm">{{ estudio.radiologoAsignadoNombre ?? '—' }}</td>
              <td class="px-5 py-3.5 text-right">
                <RouterLink :to="`/estudios/${estudio.id}`" class="btn-orb" title="Abrir estudio">
                  <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.6" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6m0 0H9m9 0v9" />
                  </svg>
                </RouterLink>
              </td>
            </tr>
            <tr v-if="cargando">
              <td colspan="8" class="text-ink-faint px-5 py-12 text-center text-sm">Cargando estudios…</td>
            </tr>
            <tr v-else-if="estudios.length === 0">
              <td colspan="8" class="text-ink-faint px-5 py-12 text-center text-sm">
                {{ hayFiltros ? 'Ningún estudio coincide con los filtros.' : 'Todavía no hay estudios cargados.' }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <Paginacion :pagina="pagina" :tamano-pagina="tamanoPagina" :total="total" @cambiar="(p) => (pagina = p)" />
    </div>

    <p v-if="error" class="text-sm text-red-700">{{ error }}</p>

    <Modal
      :abierto="modalAbierto"
      titulo="Agregar resultado de estudio"
      subtitulo="Seleccioná los archivos DICOM del estudio. Podés cargar todas las instancias de la serie a la vez."
      @cerrar="modalAbierto = false"
    >
      <FormularioResultadoEstudio @subido="alSubir" />
    </Modal>
  </div>
</template>
