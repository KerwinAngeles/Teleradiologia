<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { api } from '@/services/api'
import { useDebounce } from '@/composables/useDebounce'
import Paginacion from '@/components/Paginacion.vue'
import type { PagedResult } from '@/types/pagina'
import type { Evento, KpisEventos, TipoOperacion } from '@/types/evento'

const eventos = ref<Evento[]>([])
const kpis = ref<KpisEventos | null>(null)
const entidades = ref<string[]>([])
const cargando = ref(true)
const error = ref<string | null>(null)

const fEntidad = ref('')
const fOperacion = ref<TipoOperacion | ''>('')
const fTexto = ref('')
const textoDebounced = useDebounce(fTexto)
const ventana = ref(7)

const pagina = ref(1)
const tamanoPagina = ref(20)
const total = ref(0)

const expandido = ref<string | null>(null)

async function cargar() {
  cargando.value = true
  error.value = null
  try {
    const { data } = await api.get<PagedResult<Evento>>('/eventos', {
      params: {
        pageNumber: pagina.value,
        pageSize: tamanoPagina.value,
        entidad: fEntidad.value || undefined,
        operacion: fOperacion.value || undefined,
        texto: textoDebounced.value.trim() || undefined,
      },
    })
    eventos.value = data.items
    total.value = data.totalCount
  } catch {
    error.value = 'No se pudieron cargar los eventos.'
  } finally {
    cargando.value = false
  }
}

async function cargarKpis() {
  try {
    const { data } = await api.get<KpisEventos>('/eventos/kpis', { params: { dias: ventana.value } })
    kpis.value = data
  } catch {
    kpis.value = null
  }
}

watch([fEntidad, fOperacion, textoDebounced], () => {
  pagina.value = 1
  cargar()
})

watch(pagina, cargar)
watch(ventana, cargarKpis)

onMounted(async () => {
  await Promise.all([cargar(), cargarKpis()])
  try {
    const { data } = await api.get<string[]>('/eventos/entidades')
    entidades.value = data
  } catch {
    entidades.value = []
  }
})

const hayFiltros = computed(() => Boolean(fEntidad.value || fOperacion.value || fTexto.value.trim()))

const operacionChip: Record<TipoOperacion, string> = {
  Creacion: 'chip-informado',
  Modificacion: 'chip-informe',
  Eliminacion: 'chip-pendiente',
}

const operacionLabel: Record<TipoOperacion, string> = {
  Creacion: 'Creación',
  Modificacion: 'Modificación',
  Eliminacion: 'Eliminación',
}

const ventanas = [
  { valor: 1, etiqueta: '24 h' },
  { valor: 7, etiqueta: '7 días' },
  { valor: 30, etiqueta: '30 días' },
]

interface FilaCambio {
  campo: string
  antes: string
  despues: string
}

function detalle(evento: Evento): FilaCambio[] {
  if (!evento.cambios) return []

  try {
    const datos = JSON.parse(evento.cambios) as Record<string, unknown>

    return Object.entries(datos).map(([campo, valor]) => {
      if (valor && typeof valor === 'object' && 'despues' in (valor as object)) {
        const par = valor as { antes?: unknown; despues?: unknown }
        return { campo, antes: mostrar(par.antes), despues: mostrar(par.despues) }
      }
      return { campo, antes: '—', despues: mostrar(valor) }
    })
  } catch {
    return []
  }
}

function mostrar(valor: unknown): string {
  if (valor === null || valor === undefined || valor === '') return '—'
  const texto = String(valor)
  return texto.length > 80 ? `${texto.slice(0, 80)}…` : texto
}

function alternar(id: string) {
  expandido.value = expandido.value === id ? null : id
}

const formatoFechaHora = new Intl.DateTimeFormat('es-AR', {
  day: '2-digit',
  month: '2-digit',
  year: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
})

const brillos = ['from-coral/70', 'from-lilac/70', 'from-aqua/70', 'from-coral/50']
</script>

<template>
  <div class="stagger space-y-7">
    <div class="flex flex-wrap items-end justify-between gap-4">
      <div>
        <p class="meta-label">Configuración</p>
        <h1 class="display mt-1.5 text-3xl sm:text-4xl">Eventos</h1>
        <p class="text-ink-soft mt-2 text-sm">
          Qué se creó, modificó o eliminó, quién lo hizo y cuándo. Se registra solo, sin que ningún caso de uso
          tenga que acordarse.
        </p>
      </div>
      <div class="flex gap-2">
        <button
          v-for="v in ventanas"
          :key="v.valor"
          type="button"
          class="chip transition-colors"
          :class="ventana === v.valor ? 'chip-informe' : 'chip-neutro'"
          @click="ventana = v.valor"
        >
          {{ v.etiqueta }}
        </button>
      </div>
    </div>

    <div v-if="kpis" class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <div
        v-for="(kpi, i) in [
          { etiqueta: 'Eventos', valor: kpis.total, detalle: 'en el período' },
          { etiqueta: 'Creaciones', valor: kpis.creaciones, detalle: 'registros nuevos' },
          { etiqueta: 'Modificaciones', valor: kpis.modificaciones, detalle: 'registros editados' },
          { etiqueta: 'Usuarios activos', valor: kpis.usuariosActivos, detalle: 'hicieron cambios' },
        ]"
        :key="kpi.etiqueta"
        class="glass relative overflow-hidden p-5"
      >
        <div
          class="absolute inset-x-0 bottom-0 h-24 bg-gradient-to-t to-transparent opacity-60"
          :class="brillos[i % brillos.length]"
        />
        <div class="relative">
          <p class="meta-label">{{ kpi.etiqueta }}</p>
          <p class="mt-2 text-4xl font-light tabular-nums">{{ kpi.valor }}</p>
          <p class="text-ink-faint mt-1 text-xs">{{ kpi.detalle }}</p>
        </div>
      </div>
    </div>

    <div v-if="kpis && (kpis.porEntidad.length > 0 || kpis.porUsuario.length > 0)" class="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <div class="glass p-6">
        <p class="meta-label">Qué se tocó más</p>
        <div class="mt-4 space-y-2.5">
          <div v-for="fila in kpis.porEntidad" :key="fila.clave" class="meta-row">
            <span class="text-sm">{{ fila.clave }}</span>
            <span class="text-sm font-medium tabular-nums">{{ fila.cantidad }}</span>
          </div>
        </div>
      </div>
      <div class="glass p-6">
        <p class="meta-label">Quién hizo más cambios</p>
        <div class="mt-4 space-y-2.5">
          <div v-for="fila in kpis.porUsuario" :key="fila.clave" class="meta-row">
            <span class="text-sm">{{ fila.clave }}</span>
            <span class="text-sm font-medium tabular-nums">{{ fila.cantidad }}</span>
          </div>
        </div>
      </div>
    </div>

    <div class="glass flex flex-wrap items-center gap-3 p-4">
      <input v-model="fTexto" type="search" placeholder="Email del autor o id del registro…" class="field min-w-[220px] flex-1" />
      <select v-model="fEntidad" class="field !w-auto">
        <option value="">Todas las entidades</option>
        <option v-for="e in entidades" :key="e" :value="e">{{ e }}</option>
      </select>
      <select v-model="fOperacion" class="field !w-auto">
        <option value="">Todas las operaciones</option>
        <option value="Creacion">Creación</option>
        <option value="Modificacion">Modificación</option>
        <option value="Eliminacion">Eliminación</option>
      </select>
    </div>

    <div class="glass overflow-hidden">
      <div class="overflow-x-auto">
        <table class="min-w-full">
          <thead>
            <tr class="border-b border-[var(--color-hairline)]">
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Cuándo</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Operación</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Entidad</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Autor</th>
              <th class="px-5 py-3.5"></th>
            </tr>
          </thead>
          <tbody>
            <template v-for="evento in eventos" :key="evento.id">
              <tr
                class="cursor-pointer border-b border-[var(--color-hairline)] transition-colors hover:bg-[var(--color-superficie-suave)]"
                :class="{ 'border-0': expandido === evento.id }"
                @click="alternar(evento.id)"
              >
                <td class="text-ink-soft px-5 py-3.5 text-sm tabular-nums">
                  {{ formatoFechaHora.format(new Date(evento.timestamp)) }}
                </td>
                <td class="px-5 py-3.5">
                  <span class="chip" :class="operacionChip[evento.operacion]">
                    {{ operacionLabel[evento.operacion] }}
                  </span>
                </td>
                <td class="px-5 py-3.5">
                  <p class="text-sm font-medium">{{ evento.entidad }}</p>
                  <p class="text-ink-faint font-mono text-xs">{{ evento.entidadId.slice(0, 8) }}…</p>
                </td>
                <td class="text-ink-soft px-5 py-3.5 text-sm">{{ evento.usuarioEmail ?? '(sistema)' }}</td>
                <td class="px-5 py-3.5 text-right">
                  <svg
                    class="text-ink-faint inline h-4 w-4 transition-transform"
                    :class="{ 'rotate-180': expandido === evento.id }"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke-width="1.6"
                    stroke="currentColor"
                  >
                    <path stroke-linecap="round" stroke-linejoin="round" d="m19.5 8.25-7.5 7.5-7.5-7.5" />
                  </svg>
                </td>
              </tr>
              <tr v-if="expandido === evento.id" class="border-b border-[var(--color-hairline)]">
                <td colspan="5" class="bg-[var(--color-campo)] px-5 py-4">
                  <p class="meta-label mb-3">Detalle · {{ evento.entidadId }}</p>
                  <table v-if="detalle(evento).length > 0" class="min-w-full text-sm">
                    <thead>
                      <tr class="text-ink-faint">
                        <th class="py-1.5 pr-6 text-left text-xs font-medium">Campo</th>
                        <th class="py-1.5 pr-6 text-left text-xs font-medium">Antes</th>
                        <th class="py-1.5 text-left text-xs font-medium">Después</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="fila in detalle(evento)" :key="fila.campo" class="align-top">
                        <td class="py-1.5 pr-6 font-medium">{{ fila.campo }}</td>
                        <td class="text-ink-soft py-1.5 pr-6 break-all">{{ fila.antes }}</td>
                        <td class="py-1.5 break-all">{{ fila.despues }}</td>
                      </tr>
                    </tbody>
                  </table>
                  <p v-else class="text-ink-faint text-sm">Sin detalle de campos.</p>
                </td>
              </tr>
            </template>

            <tr v-if="cargando">
              <td colspan="5" class="text-ink-faint px-5 py-12 text-center text-sm">Cargando eventos…</td>
            </tr>
            <tr v-else-if="eventos.length === 0">
              <td colspan="5" class="text-ink-faint px-5 py-12 text-center text-sm">
                {{ hayFiltros ? 'Ningún evento coincide con los filtros.' : 'Todavía no hay eventos registrados.' }}
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
