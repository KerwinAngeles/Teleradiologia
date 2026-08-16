<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { isAxiosError } from 'axios'
import { api } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import Paginacion from '@/components/Paginacion.vue'
import { useDebounce } from '@/composables/useDebounce'
import type { EstablecimientoCatalogo, Hospital } from '@/types/hospital'
import type { PagedResult } from '@/types/pagina'

const toasts = useToastStore()

const hospitales = ref<Hospital[]>([])
const cargando = ref(true)
const error = ref<string | null>(null)

const pagina = ref(1)
const tamanoPagina = ref(20)
const total = ref(0)
const fTexto = ref('')
const fProvincia = ref('')
const textoDebounced = useDebounce(fTexto)

const panelAbierto = ref(false)
const origen = ref<'catalogo' | 'manual'>('catalogo')
const guardando = ref(false)
const errorForm = ref<string | null>(null)

const provincias = ref<string[]>([])
const busqueda = ref('')
const busquedaDebounced = useDebounce(busqueda)
const provinciaFiltro = ref('')
const tipoFiltro = ref('')
const tipos = ref<string[]>([])
const resultados = ref<EstablecimientoCatalogo[]>([])
const catPagina = ref(1)
const catTamanoPagina = ref(10)
const catTotal = ref(0)
const buscando = ref(false)

const nombre = ref('')
const provincia = ref('')
const municipio = ref('')
const emailContacto = ref('')
const codigoExterno = ref<number | null>(null)
const slaStat = ref<number | null>(null)
const slaUrgente = ref<number | null>(null)
const slaRutina = ref<number | null>(null)

async function cargar() {
  cargando.value = true
  error.value = null
  try {
    const { data } = await api.get<PagedResult<Hospital>>('/hospitales/buscar', {
      params: {
        pageNumber: pagina.value,
        pageSize: tamanoPagina.value,
        texto: textoDebounced.value.trim() || undefined,
        provincia: fProvincia.value || undefined,
      },
    })
    hospitales.value = data.items
    total.value = data.totalCount
  } catch {
    error.value = 'No se pudieron cargar los hospitales.'
  } finally {
    cargando.value = false
  }
}

watch([textoDebounced, fProvincia], () => {
  pagina.value = 1
  cargar()
})

watch(pagina, cargar)

onMounted(async () => {
  await Promise.all([cargar(), cargarCodigos()])
  try {
    const [prov, tip] = await Promise.all([
      api.get<string[]>('/hospitales/catalogo/provincias'),
      api.get<string[]>('/hospitales/catalogo/tipos'),
    ])
    provincias.value = prov.data
    tipos.value = tip.data
  } catch {
    // El catálogo es opcional: sin él el alta manual sigue funcionando.
  }
})

const codigosRegistrados = ref<Set<number>>(new Set())

// Se pide sobre el total: con paginación, la página visible no alcanza para saber
// qué establecimientos ya están cargados.
async function cargarCodigos() {
  try {
    const { data } = await api.get<Hospital[]>('/hospitales')
    codigosRegistrados.value = new Set(
      data.map((h) => h.codigoExterno).filter((c): c is number => c !== null),
    )
  } catch {
    codigosRegistrados.value = new Set()
  }
}

const yaRegistrados = computed(() => codigosRegistrados.value)

watch([busquedaDebounced, provinciaFiltro, tipoFiltro], () => {
  catPagina.value = 1
  buscarEnCatalogo()
})

watch(catPagina, buscarEnCatalogo)

async function buscarEnCatalogo() {
  if (!busqueda.value.trim() && !provinciaFiltro.value && !tipoFiltro.value) {
    resultados.value = []
    catTotal.value = 0
    return
  }

  buscando.value = true
  try {
    const { data } = await api.get<PagedResult<EstablecimientoCatalogo>>('/hospitales/catalogo', {
      params: {
        pageNumber: catPagina.value,
        pageSize: catTamanoPagina.value,
        texto: busqueda.value.trim() || undefined,
        provincia: provinciaFiltro.value || undefined,
        tipo: tipoFiltro.value || undefined,
      },
    })
    resultados.value = data.items
    catTotal.value = data.totalCount
  } catch {
    toasts.error('No se pudo buscar en el catálogo.')
  } finally {
    buscando.value = false
  }
}

function abrirPanel() {
  origen.value = 'catalogo'
  busqueda.value = ''
  provinciaFiltro.value = ''
  tipoFiltro.value = ''
  resultados.value = []
  catTotal.value = 0
  catPagina.value = 1
  limpiarFormulario()
  errorForm.value = null
  panelAbierto.value = true
}

function limpiarFormulario() {
  nombre.value = ''
  provincia.value = ''
  municipio.value = ''
  emailContacto.value = ''
  codigoExterno.value = null
  slaStat.value = null
  slaUrgente.value = null
  slaRutina.value = null
}

function elegirDelCatalogo(establecimiento: EstablecimientoCatalogo) {
  nombre.value = establecimiento.nombre
  provincia.value = establecimiento.provincia ?? ''
  municipio.value = establecimiento.municipio ?? ''
  codigoExterno.value = establecimiento.codigo
  emailContacto.value = ''
  origen.value = 'manual'
}

async function guardar() {
  errorForm.value = null
  guardando.value = true
  try {
    await api.post('/hospitales', {
      nombre: nombre.value.trim(),
      codigoExterno: codigoExterno.value,
      provincia: provincia.value.trim() || null,
      municipio: municipio.value.trim() || null,
      emailContacto: emailContacto.value.trim() || null,
      slaStatMinutos: slaStat.value,
      slaUrgenteMinutos: slaUrgente.value,
      slaRutinaMinutos: slaRutina.value,
    })
    panelAbierto.value = false
    await Promise.all([cargar(), cargarCodigos()])
    toasts.exito(`${nombre.value.trim()} ya está disponible para recibir estudios.`)
  } catch (e) {
    const mensaje: string =
      isAxiosError(e) && e.response?.data?.detail ? e.response.data.detail : 'No se pudo dar de alta el hospital.'
    errorForm.value = mensaje
    toasts.error(mensaje)
  } finally {
    guardando.value = false
  }
}
</script>

<template>
  <div class="stagger space-y-7">
    <div class="flex flex-wrap items-end justify-between gap-4">
      <div>
        <RouterLink to="/configuracion" class="meta-label hover:text-ink transition-colors">
          ← Configuración
        </RouterLink>
        <h1 class="display mt-1.5 text-3xl sm:text-4xl">Catálogo de hospitales</h1>
        <p class="text-ink-soft mt-2 text-sm">
          Los centros que envían estudios. Cada uno ve solo lo suyo: pacientes, estudios e informes quedan aislados.
        </p>
      </div>
      <button type="button" class="btn-ink" @click="abrirPanel">
        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
        </svg>
        Agregar hospital
      </button>
    </div>

    <Transition name="fade-slide">
      <div v-if="panelAbierto" class="glass animate-none space-y-5 p-7">
        <div class="flex flex-wrap items-center justify-between gap-3">
          <p class="meta-label">Alta de hospital</p>
          <div class="flex gap-2">
            <button
              type="button"
              class="chip"
              :class="origen === 'catalogo' ? 'chip-informe' : 'chip-neutro'"
              @click="origen = 'catalogo'"
            >
              Buscar en el catálogo
            </button>
            <button
              type="button"
              class="chip"
              :class="origen === 'manual' ? 'chip-informe' : 'chip-neutro'"
              @click="origen = 'manual'"
            >
              Cargar a mano
            </button>
          </div>
        </div>

        <div v-if="origen === 'catalogo'" class="space-y-4">
          <p class="text-ink-soft text-sm">
            Listado oficial del Ministerio de Salud Pública. Los centros privados no figuran: esos se cargan a mano.
          </p>

          <div class="grid grid-cols-1 gap-3 sm:grid-cols-[1fr_220px_220px]">
            <input v-model="busqueda" type="search" placeholder="Nombre del centro…" class="field" />
            <select v-model="provinciaFiltro" class="field">
              <option value="">Todas las provincias</option>
              <option v-for="p in provincias" :key="p" :value="p">{{ p }}</option>
            </select>
            <select v-model="tipoFiltro" class="field">
              <option value="">Todos los tipos</option>
              <option v-for="tp in tipos" :key="tp" :value="tp">{{ tp }}</option>
            </select>
          </div>

          <div class="overflow-hidden rounded-2xl border border-[var(--color-hairline)]">
            <p v-if="buscando" class="text-ink-faint px-4 py-6 text-center text-sm">Buscando…</p>
            <p
              v-else-if="resultados.length === 0"
              class="text-ink-faint px-4 py-6 text-center text-sm"
            >
              Escribí un nombre o elegí una provincia o tipo para buscar.
            </p>
            <button
              v-for="e in resultados"
              v-else
              :key="e.codigo"
              type="button"
              :disabled="yaRegistrados.has(e.codigo)"
              class="flex w-full items-start justify-between gap-4 border-b border-[var(--color-hairline)] px-4 py-3 text-left transition-colors last:border-0 hover:bg-[var(--color-superficie-suave)] disabled:cursor-not-allowed disabled:opacity-45"
              @click="elegirDelCatalogo(e)"
            >
              <span>
                <span class="block text-sm font-medium">{{ e.nombre }}</span>
                <span class="text-ink-faint text-xs">
                  {{ [e.municipio, e.provincia].filter(Boolean).join(', ') }}
                </span>
              </span>
              <span class="chip chip-neutro flex-none">
                {{ yaRegistrados.has(e.codigo) ? 'Ya cargado' : (e.tipo ?? '—') }}
              </span>
            </button>

            <Paginacion
              :pagina="catPagina"
              :tamano-pagina="catTamanoPagina"
              :total="catTotal"
              @cambiar="(p) => (catPagina = p)"
            />
          </div>
        </div>

        <form v-else class="space-y-4" @submit.prevent="guardar">
          <p v-if="codigoExterno" class="text-ink-soft text-sm">
            Tomado del catálogo oficial (código {{ codigoExterno }}).
            <button type="button" class="underline underline-offset-4" @click="limpiarFormulario">Descartar</button>
          </p>

          <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div class="sm:col-span-2">
              <label class="meta-label mb-1.5 block" for="nombre">Nombre</label>
              <input id="nombre" v-model="nombre" type="text" required maxlength="200" class="field" />
            </div>
            <div>
              <label class="meta-label mb-1.5 block" for="provincia">Provincia</label>
              <input id="provincia" v-model="provincia" type="text" maxlength="80" class="field" />
            </div>
            <div>
              <label class="meta-label mb-1.5 block" for="municipio">Municipio</label>
              <input id="municipio" v-model="municipio" type="text" maxlength="120" class="field" />
            </div>
            <div class="sm:col-span-2">
              <label class="meta-label mb-1.5 block" for="email">Email de contacto</label>
              <input id="email" v-model="emailContacto" type="email" maxlength="256" class="field" />
            </div>
          </div>

          <div class="rounded-2xl border border-[var(--color-hairline)] p-5">
            <p class="meta-label">Plazos contratados</p>
            <p class="text-ink-soft mt-1.5 text-sm">
              Minutos para entregar el informe según la urgencia. Vacío usa el plazo general
              (30 min / 2 h / 24 h).
            </p>
            <div class="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-3">
              <div>
                <label class="meta-label mb-1.5 block" for="sla-stat">STAT</label>
                <input id="sla-stat" v-model.number="slaStat" type="number" min="1" max="20160" placeholder="30" class="field" />
              </div>
              <div>
                <label class="meta-label mb-1.5 block" for="sla-urgente">Urgente</label>
                <input id="sla-urgente" v-model.number="slaUrgente" type="number" min="1" max="20160" placeholder="120" class="field" />
              </div>
              <div>
                <label class="meta-label mb-1.5 block" for="sla-rutina">Rutina</label>
                <input id="sla-rutina" v-model.number="slaRutina" type="number" min="1" max="20160" placeholder="1440" class="field" />
              </div>
            </div>
          </div>

          <p v-if="errorForm" class="rounded-xl bg-red-500/10 px-3 py-2 text-sm text-red-700">{{ errorForm }}</p>

          <div class="flex gap-3">
            <button type="submit" :disabled="guardando || !nombre.trim()" class="btn-ink">
              {{ guardando ? 'Guardando…' : 'Dar de alta' }}
            </button>
            <button type="button" class="btn-ghost" @click="panelAbierto = false">Cancelar</button>
          </div>
        </form>
      </div>
    </Transition>

    <div class="glass flex flex-wrap items-center gap-3 p-4">
      <div class="relative min-w-[220px] flex-1">
        <input v-model="fTexto" type="search" placeholder="Nombre del hospital…" class="field" />
      </div>
      <select v-model="fProvincia" class="field !w-auto">
        <option value="">Todas las provincias</option>
        <option v-for="p in provincias" :key="p" :value="p">{{ p }}</option>
      </select>
    </div>

    <div class="glass overflow-hidden">
      <div class="overflow-x-auto">
        <table class="min-w-full">
          <thead>
            <tr class="border-b border-[var(--color-hairline)]">
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Hospital</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Ubicación</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Contacto</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Plazos (STAT / Urg. / Rut.)</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Origen</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="hospital in hospitales"
              :key="hospital.id"
              class="border-b border-[var(--color-hairline)] transition-colors last:border-0 hover:bg-white/55"
            >
              <td class="px-5 py-3.5 text-sm font-medium">{{ hospital.nombre }}</td>
              <td class="text-ink-soft px-5 py-3.5 text-sm">
                {{ [hospital.municipio, hospital.provincia].filter(Boolean).join(', ') || '—' }}
              </td>
              <td class="text-ink-soft px-5 py-3.5 text-sm">{{ hospital.emailContacto ?? '—' }}</td>
              <td class="text-ink-soft px-5 py-3.5 text-sm tabular-nums">
                {{ hospital.slaStatMinutos ?? 30 }} / {{ hospital.slaUrgenteMinutos ?? 120 }} /
                {{ hospital.slaRutinaMinutos ?? 1440 }} min
              </td>
              <td class="px-5 py-3.5">
                <span class="chip" :class="hospital.codigoExterno ? 'chip-informado' : 'chip-neutro'">
                  {{ hospital.codigoExterno ? 'Catálogo MSP' : 'Manual' }}
                </span>
              </td>
            </tr>
            <tr v-if="cargando">
              <td colspan="5" class="text-ink-faint px-5 py-12 text-center text-sm">Cargando hospitales…</td>
            </tr>
            <tr v-else-if="hospitales.length === 0">
              <td colspan="5" class="text-ink-faint px-5 py-12 text-center text-sm">
                Todavía no hay hospitales cargados.
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
