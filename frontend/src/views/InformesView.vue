<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { api } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useDebounce } from '@/composables/useDebounce'
import Paginacion from '@/components/Paginacion.vue'
import type { InformeListado, EstadoInforme } from '@/types/informe'
import type { PagedResult } from '@/types/pagina'

const auth = useAuthStore()

const informes = ref<InformeListado[]>([])
const cargando = ref(true)
const error = ref<string | null>(null)

const pagina = ref(1)
const tamanoPagina = ref(20)
const total = ref(0)

const fTexto = ref('')
const fEstado = ref<EstadoInforme | ''>('')
const fDesde = ref('')
const fHasta = ref('')

const textoDebounced = useDebounce(fTexto)

const hayFiltros = computed(() => !!fTexto.value || !!fEstado.value || !!fDesde.value || !!fHasta.value)

function limpiarFiltros() {
  fTexto.value = ''
  fEstado.value = ''
  fDesde.value = ''
  fHasta.value = ''
}

async function cargar() {
  cargando.value = true
  error.value = null
  try {
    const { data } = await api.get<PagedResult<InformeListado>>('/informes', {
      params: {
        pageNumber: pagina.value,
        pageSize: tamanoPagina.value,
        texto: textoDebounced.value.trim() || undefined,
        estado: fEstado.value || undefined,
        desde: fDesde.value || undefined,
        // El input de fecha da el día suelto: se lleva al final para que el día elegido entre.
        hasta: fHasta.value ? `${fHasta.value}T23:59:59` : undefined,
      },
    })
    informes.value = data.items
    total.value = data.totalCount
  } catch {
    error.value = 'No se pudieron cargar los informes.'
  } finally {
    cargando.value = false
  }
}

watch([textoDebounced, fEstado, fDesde, fHasta], () => {
  pagina.value = 1
  cargar()
})

watch(pagina, cargar)

onMounted(cargar)

// Cada rol ve un recorte distinto y conviene decirlo: si no, un radiólogo puede creer
// que la plataforma tiene menos informes de los que tiene.
const alcance = computed(() => {
  switch (auth.usuario?.rol) {
    case 'Admin':
      return 'Todos los informes de la plataforma'
    case 'Radiologo':
      return 'Los informes que redactaste'
    default:
      return 'Los informes de los estudios que subiste'
  }
})

const formatoFecha = new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
const formatoFechaHora = new Intl.DateTimeFormat('es-AR', { dateStyle: 'short', timeStyle: 'short' })
</script>

<template>
  <div class="stagger space-y-7">
    <div>
      <p class="meta-label">Documentos</p>
      <h1 class="display mt-1.5 text-3xl sm:text-4xl">Informes</h1>
      <p class="text-ink-soft mt-2 text-sm">{{ alcance }}</p>
    </div>

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

      <select v-model="fEstado" class="field !w-auto" aria-label="Estado del informe">
        <option value="">Todos los estados</option>
        <option value="Firmado">Firmado</option>
        <option value="Borrador">Borrador</option>
      </select>

      <label class="text-ink-soft flex items-center gap-2 text-xs">
        Desde
        <input v-model="fDesde" type="date" class="field !w-auto" />
      </label>
      <label class="text-ink-soft flex items-center gap-2 text-xs">
        Hasta
        <input v-model="fHasta" type="date" class="field !w-auto" />
      </label>

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
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Estudio</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Tipo</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Radiólogo</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Estado</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Firmado</th>
              <th class="px-5 py-3.5"></th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="informe in informes"
              :key="informe.id"
              class="border-b border-[var(--color-hairline)] transition-colors last:border-0 hover:bg-white/55"
            >
              <td class="px-5 py-3.5">
                <p class="text-sm font-medium">{{ informe.pacienteNombre }}</p>
                <p class="text-ink-faint text-xs">{{ informe.pacienteDocumento }}</p>
              </td>
              <td class="text-ink-soft px-5 py-3.5 text-sm">{{ informe.modalidad }}</td>
              <td class="text-ink-soft px-5 py-3.5 text-sm">{{ informe.hospitalNombre }}</td>
              <td class="text-ink-soft px-5 py-3.5 text-sm tabular-nums">
                {{ formatoFecha.format(new Date(informe.fechaEstudio)) }}
              </td>
              <td class="px-5 py-3.5">
                <span class="chip chip-neutro">{{ informe.esAdenda ? 'Adenda' : 'Original' }}</span>
              </td>
              <td class="text-ink-soft px-5 py-3.5 text-sm">{{ informe.radiologoNombre }}</td>
              <td class="px-5 py-3.5">
                <span class="chip" :class="informe.estado === 'Firmado' ? 'chip-informado' : 'chip-pendiente'">
                  {{ informe.estado === 'Firmado' ? 'Firmado' : 'Borrador' }}
                </span>
              </td>
              <td class="text-ink-soft px-5 py-3.5 text-sm tabular-nums">
                {{ informe.firmadoAt ? formatoFechaHora.format(new Date(informe.firmadoAt)) : '—' }}
              </td>
              <td class="px-5 py-3.5">
                <div class="flex items-center justify-end gap-2">
                  <RouterLink :to="`/informes/${informe.id}`" class="btn-ink !px-3.5 !py-1.5 !text-xs">
                    Ver hoja
                  </RouterLink>
                  <RouterLink
                    :to="`/estudios/${informe.estudioId}`"
                    class="btn-orb"
                    title="Abrir el estudio"
                  >
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.6" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6m0 0H9m9 0v9" />
                    </svg>
                  </RouterLink>
                </div>
              </td>
            </tr>

            <tr v-if="cargando">
              <td colspan="9" class="text-ink-faint px-5 py-12 text-center text-sm">Cargando informes…</td>
            </tr>
            <tr v-else-if="informes.length === 0">
              <td colspan="9" class="text-ink-faint px-5 py-12 text-center text-sm">
                {{ hayFiltros ? 'Ningún informe coincide con los filtros.' : 'Todavía no hay informes.' }}
              </td>
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
