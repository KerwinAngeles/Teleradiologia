<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { api } from '@/services/api'
import { useDebounce } from '@/composables/useDebounce'
import { useNotificacionesStore } from '@/stores/notificaciones'
import Paginacion from '@/components/Paginacion.vue'
import type { PagedResult } from '@/types/pagina'
import type { Notificacion, TipoNotificacion } from '@/types/notificacion'

const router = useRouter()
const store = useNotificacionesStore()

const notificaciones = ref<Notificacion[]>([])
const cargando = ref(true)
const error = ref<string | null>(null)

const fTipo = ref<TipoNotificacion | ''>('')
const fSoloNoLeidas = ref(false)
const fTexto = ref('')
const textoDebounced = useDebounce(fTexto)
const fDesde = ref('')
const fHasta = ref('')

const pagina = ref(1)
const tamanoPagina = ref(20)
const total = ref(0)

async function cargar() {
  cargando.value = true
  error.value = null
  try {
    const { data } = await api.get<PagedResult<Notificacion>>('/notificaciones', {
      params: {
        pageNumber: pagina.value,
        pageSize: tamanoPagina.value,
        tipo: fTipo.value || undefined,
        soloNoLeidas: fSoloNoLeidas.value || undefined,
        texto: textoDebounced.value.trim() || undefined,
        desde: fDesde.value ? new Date(fDesde.value).toISOString() : undefined,
        // El input date da el arranque del día; se suma uno para que "hasta" lo incluya.
        hasta: fHasta.value ? new Date(new Date(fHasta.value).getTime() + 86400000).toISOString() : undefined,
      },
    })
    notificaciones.value = data.items
    total.value = data.totalCount
  } catch {
    error.value = 'No se pudieron cargar las notificaciones.'
  } finally {
    cargando.value = false
  }
}

watch([fTipo, fSoloNoLeidas, textoDebounced, fDesde, fHasta], () => {
  pagina.value = 1
  cargar()
})

watch(pagina, cargar)

onMounted(cargar)

const hayFiltros = computed(
  () => Boolean(fTipo.value || fSoloNoLeidas.value || fTexto.value.trim() || fDesde.value || fHasta.value),
)

function limpiarFiltros() {
  fTipo.value = ''
  fSoloNoLeidas.value = false
  fTexto.value = ''
  fDesde.value = ''
  fHasta.value = ''
}

async function abrir(n: Notificacion) {
  if (!n.leidaAt) {
    await store.marcarLeida(n.id)
    n.leidaAt = new Date().toISOString()
  }
  if (n.estudioId) router.push(`/estudios/${n.estudioId}`)
}

async function marcarTodas() {
  await store.marcarTodasLeidas()
  await cargar()
}

const tipoLabel: Record<TipoNotificacion, string> = {
  EstudioNuevo: 'Estudio nuevo',
  EstudioUrgente: 'Estudio urgente',
  InformeFirmado: 'Informe firmado',
  SlaPorVencer: 'SLA por vencer',
}

const tipoChip: Record<TipoNotificacion, string> = {
  EstudioNuevo: 'chip-informe',
  EstudioUrgente: 'chip-pendiente',
  InformeFirmado: 'chip-informado',
  SlaPorVencer: 'chip-pendiente',
}

const formatoFechaHora = new Intl.DateTimeFormat('es-AR', {
  day: '2-digit',
  month: '2-digit',
  year: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
})
</script>

<template>
  <div class="stagger space-y-7">
    <div class="flex flex-wrap items-end justify-between gap-4">
      <div>
        <p class="meta-label">Bandeja</p>
        <h1 class="display mt-1.5 text-3xl sm:text-4xl">Notificaciones</h1>
        <p class="text-ink-soft mt-2 text-sm">
          Estudios que llegaron para tu evaluación.
          <span :class="store.conectado ? 'text-[var(--color-estado-informado)]' : 'text-ink-faint'">
            {{ store.conectado ? 'Avisos en vivo activos.' : 'Sin conexión en vivo — se actualizan al recargar.' }}
          </span>
        </p>
      </div>
      <button
        v-if="store.noLeidas > 0"
        type="button"
        class="btn-ghost"
        @click="marcarTodas"
      >
        Marcar todas leídas ({{ store.noLeidas }})
      </button>
    </div>

    <div class="glass flex flex-wrap items-end gap-3 p-4">
      <div class="min-w-[200px] flex-1">
        <label class="meta-label mb-1.5 block" for="f-texto">Buscar</label>
        <input id="f-texto" v-model="fTexto" type="search" placeholder="Paciente o texto del aviso…" class="field" />
      </div>
      <div>
        <label class="meta-label mb-1.5 block" for="f-tipo">Tipo</label>
        <select id="f-tipo" v-model="fTipo" class="field !w-auto">
          <option value="">Todos</option>
          <option value="EstudioNuevo">Estudio nuevo</option>
          <option value="EstudioUrgente">Estudio urgente</option>
          <option value="InformeFirmado">Informe firmado</option>
          <option value="SlaPorVencer">SLA por vencer</option>
        </select>
      </div>
      <div>
        <label class="meta-label mb-1.5 block" for="f-desde">Desde</label>
        <input id="f-desde" v-model="fDesde" type="date" class="field !w-auto" />
      </div>
      <div>
        <label class="meta-label mb-1.5 block" for="f-hasta">Hasta</label>
        <input id="f-hasta" v-model="fHasta" type="date" class="field !w-auto" />
      </div>
      <label class="flex cursor-pointer items-center gap-2 py-2.5 text-sm">
        <input v-model="fSoloNoLeidas" type="checkbox" class="h-4 w-4 rounded" />
        Solo no leídas
      </label>
      <button v-if="hayFiltros" type="button" class="btn-ghost !py-2 !text-xs" @click="limpiarFiltros">
        Limpiar
      </button>
    </div>

    <div class="glass overflow-hidden">
      <div class="overflow-x-auto">
        <table class="min-w-full">
          <thead>
            <tr class="border-b border-[var(--color-hairline)]">
              <th class="meta-label px-5 py-3.5 text-left font-semibold"></th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Cuándo</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Tipo</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Aviso</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Paciente</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Hospital</th>
              <th class="px-5 py-3.5"></th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="n in notificaciones"
              :key="n.id"
              class="cursor-pointer border-b border-[var(--color-hairline)] transition-colors last:border-0 hover:bg-[var(--color-superficie-suave)]"
              :class="{ 'font-medium': !n.leidaAt }"
              @click="abrir(n)"
            >
              <td class="px-5 py-3.5">
                <span
                  class="block h-2 w-2 rounded-full"
                  :class="n.leidaAt ? 'bg-transparent' : 'bg-[var(--color-estado-informe)]'"
                  :title="n.leidaAt ? 'Leída' : 'Sin leer'"
                />
              </td>
              <td class="text-ink-soft px-5 py-3.5 text-sm tabular-nums">
                {{ formatoFechaHora.format(new Date(n.createdAt)) }}
              </td>
              <td class="px-5 py-3.5">
                <span class="chip" :class="tipoChip[n.tipo]">{{ tipoLabel[n.tipo] }}</span>
              </td>
              <td class="px-5 py-3.5">
                <p class="text-sm">{{ n.titulo }}</p>
                <p class="text-ink-faint text-xs">{{ n.mensaje }}</p>
              </td>
              <td class="text-ink-soft px-5 py-3.5 text-sm">
                {{ n.pacienteNombre ?? '—' }}
                <span v-if="n.modalidad" class="text-ink-faint">· {{ n.modalidad }}</span>
              </td>
              <td class="text-ink-soft px-5 py-3.5 text-sm">{{ n.hospitalNombre ?? '—' }}</td>
              <td class="px-5 py-3.5 text-right">
                <svg
                  v-if="n.estudioId"
                  class="text-ink-faint inline h-4 w-4"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke-width="1.6"
                  stroke="currentColor"
                >
                  <path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6m0 0H9m9 0v9" />
                </svg>
              </td>
            </tr>
            <tr v-if="cargando">
              <td colspan="7" class="text-ink-faint px-5 py-12 text-center text-sm">Cargando notificaciones…</td>
            </tr>
            <tr v-else-if="notificaciones.length === 0">
              <td colspan="7" class="text-ink-faint px-5 py-12 text-center text-sm">
                {{ hayFiltros ? 'Ninguna notificación coincide con los filtros.' : 'Todavía no tenés notificaciones.' }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <Paginacion :pagina="pagina" :tamano-pagina="tamanoPagina" :total="total" @cambiar="(p) => (pagina = p)" />
    </div>

    <p v-if="error" class="text-sm text-red-700">{{ error }}</p>
  </div>
</template>
