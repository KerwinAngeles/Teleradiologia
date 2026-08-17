<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, defineAsyncComponent } from 'vue'
import { useRoute } from 'vue-router'
import { api } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import { useNavegacionStore } from '@/stores/navegacion'
import { tomarPrecarga } from '@/services/precargaEstudio'
import { useReloj, formatearRestante } from '@/composables/useReloj'
import PanelEstudio from '@/components/visor/PanelEstudio.vue'
import type { Estudio, ImagenEstudio, EstadoSla } from '@/types/estudio'
import type { Informe } from '@/types/informe'

// El guard de la ruta ya bajó este chunk antes de dejar navegar, así que acá está
// resuelto al instante.
const DicomViewer = defineAsyncComponent(() => import('@/components/DicomViewer.vue'))

const route = useRoute()
const auth = useAuthStore()
const toasts = useToastStore()
const navegacion = useNavegacionStore()
const { ahora } = useReloj()

// La pantalla de carga viene encendida desde el router y la apagamos nosotros: la
// ruta está marcada `cargaPropia`. Se apaga cuando hay algo que mirar, no antes.
let pantallaLiberada = false
const datosListos = ref(false)

function liberarPantalla() {
  if (pantallaLiberada) return
  pantallaLiberada = true
  navegacion.terminar()
}

function visorListo() {
  if (datosListos.value) liberarPantalla()
}

// Red de seguridad: la pantalla de carga tapa todo, así que ningún fallo silencioso
// —un chunk que no baja, Cornerstone que no arranca— puede dejarla puesta.
const TOPE_ESPERA_MS = 8000
let temporizadorTope: ReturnType<typeof setTimeout> | null = null

const estudioId = computed(() => route.params.id as string)

const estudio = ref<Estudio | null>(null)
const imagenes = ref<ImagenEstudio[]>([])
const informes = ref<Informe[]>([])
const cargando = ref(true)
const error = ref<string | null>(null)
const tomando = ref(false)

function asentarDatos(est: Estudio, imgs: ImagenEstudio[], infs: Informe[]) {
  estudio.value = est
  imagenes.value = imgs
  informes.value = infs
  datosListos.value = true
  cargando.value = false
  // Sin imágenes no hay visor que espere: se destapa acá.
  if (imgs.length === 0) liberarPantalla()
}

async function cargarEstudio() {
  cargando.value = true
  error.value = null
  try {
    // Camino normal: el guard de la ruta ya lanzó la carga antes de dejar navegar, y
    // casi siempre ya está resuelta. Si se pasó del tope, se sigue esperando la misma
    // petición en vez de duplicarla.
    const precarga = tomarPrecarga(estudioId.value)

    const datos =
      precarga ??
      Promise.all([
        api.get<Estudio>(`/estudios/${estudioId.value}`),
        api.get<ImagenEstudio[]>(`/estudios/${estudioId.value}/imagenes`),
        api.get<Informe[]>(`/estudios/${estudioId.value}/informes`),
      ]).then(([est, imgs, infs]) => ({
        estudio: est.data,
        imagenes: imgs.data,
        informes: infs.data,
      }))

    const { estudio: est, imagenes: imgs, informes: infs } = await datos
    asentarDatos(est, imgs, infs)
  } catch {
    error.value = 'No se pudo cargar el estudio.'
    cargando.value = false
    liberarPantalla()
  }
}

async function actualizarEstudioEInformes() {
  const [{ data: est }, { data: infs }] = await Promise.all([
    api.get<Estudio>(`/estudios/${estudioId.value}`),
    api.get<Informe[]>(`/estudios/${estudioId.value}/informes`),
  ])
  estudio.value = est
  informes.value = infs
}

async function tomar() {
  tomando.value = true
  try {
    await api.post(`/estudios/${estudioId.value}/tomar`)
    await actualizarEstudioEInformes()
    toasts.exito('Tomaste el estudio. Ya podés informarlo.')
  } catch {
    toasts.error('No se pudo tomar el estudio — puede que otro radiólogo ya lo haya tomado.')
  } finally {
    tomando.value = false
  }
}

onMounted(() => {
  temporizadorTope = setTimeout(liberarPantalla, TOPE_ESPERA_MS)
  void cargarEstudio()
})

onUnmounted(() => {
  if (temporizadorTope) clearTimeout(temporizadorTope)
  // Si se sale antes de estar listo, no dejamos la pantalla encendida para siempre.
  liberarPantalla()
})

const esRadiologo = computed(() => auth.usuario?.rol === 'Radiologo')
const esMio = computed(() => estudio.value?.radiologoAsignadoId === auth.usuario?.id)
const puedeTomar = computed(() => esRadiologo.value && estudio.value?.estado === 'Pendiente')
const puedeInformar = computed(() => esRadiologo.value && esMio.value && estudio.value?.estado !== 'Informado')

const estadoChip: Record<string, string> = {
  Pendiente: 'chip-pendiente',
  EnInforme: 'chip-informe',
  Informado: 'chip-informado',
}
const estadoLabel: Record<string, string> = {
  Pendiente: 'Pendiente',
  EnInforme: 'En informe',
  Informado: 'Informado',
}

const slaChip: Record<EstadoSla, string> = {
  EnPlazo: 'chip-informado',
  PorVencer: 'chip-pendiente',
  Vencido: 'chip-vencido',
  Cumplido: 'chip-informado',
  Incumplido: 'chip-vencido',
}

const inicialPaciente = computed(() => estudio.value?.pacienteNombre.charAt(0).toUpperCase() ?? '?')
const formatoFecha = new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })

const cuadrosTotales = computed(() =>
  imagenes.value.reduce((suma, imagen) => suma + (imagen.numeroDeCuadros > 1 ? imagen.numeroDeCuadros : 1), 0),
)

// La línea técnica del encabezado: lo que el radiólogo necesita para saber que
// está mirando el estudio correcto, sin abrir la pestaña de metadatos.
const lineaTecnica = computed(() => {
  const e = estudio.value
  if (!e) return ''
  return [e.modalidad, e.descripcionEstudio, e.hospitalNombre, formatoFecha.format(new Date(e.fechaEstudio))]
    .filter(Boolean)
    .join(' · ')
})
</script>

<template>
  <div class="flex h-full flex-col gap-3">
    <p v-if="error" class="glass p-5 text-sm text-red-700">{{ error }}</p>

    <template v-else-if="estudio">
      <!-- Cabecera de paciente -->
      <header class="glass flex flex-none flex-wrap items-center gap-x-4 gap-y-3 px-4 py-3">
        <RouterLink to="/" class="btn-orb !h-9 !w-9 flex-none" title="Volver al worklist">
          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.6" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
          </svg>
        </RouterLink>

        <span class="avatar-ring h-10 w-10 flex-none">
          <span class="text-sm font-semibold">{{ inicialPaciente }}</span>
        </span>

        <div class="min-w-0">
          <div class="flex flex-wrap items-baseline gap-x-2.5">
            <h1 class="truncate text-base leading-tight font-semibold">{{ estudio.pacienteNombre }}</h1>
            <span class="text-ink-faint font-mono text-[0.6875rem]">{{ estudio.pacienteDocumento }}</span>
          </div>
          <p class="text-ink-soft truncate text-xs">{{ lineaTecnica }}</p>
        </div>

        <div class="flex flex-wrap items-center gap-1.5">
          <span class="chip chip-neutro">{{ estudio.modalidad }}</span>
          <span class="chip chip-neutro">
            {{ cuadrosTotales }} {{ cuadrosTotales === 1 ? 'corte' : 'cortes' }}
          </span>
          <span class="chip" :class="estadoChip[estudio.estado]">{{ estadoLabel[estudio.estado] }}</span>
          <span class="chip" :class="slaChip[estudio.estadoSla]">
            {{
              estudio.estadoSla === 'Cumplido'
                ? 'A tiempo'
                : estudio.estadoSla === 'Incumplido'
                  ? 'Fuera de plazo'
                  : formatearRestante(estudio.fechaLimite, ahora)
            }}
          </span>
        </div>

        <div class="ml-auto flex flex-wrap items-center gap-2">
          <button
            v-if="puedeTomar"
            type="button"
            :disabled="tomando"
            class="btn-ink !py-2 !text-xs"
            @click="tomar"
          >
            {{ tomando ? 'Tomando…' : 'Tomar estudio' }}
          </button>
          <RouterLink v-if="puedeInformar" :to="`/estudios/${estudioId}/informe`" class="btn-ink !py-2 !text-xs">
            <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" d="m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652L10.582 16.07a4.5 4.5 0 0 1-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 0 1 1.13-1.897l8.932-8.931Z" />
            </svg>
            Abrir editor de informe
          </RouterLink>
        </div>
      </header>

      <!-- Escenario y panel -->
      <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto xl:flex-row xl:overflow-visible">
        <DicomViewer
          class="max-xl:h-[62vh] max-xl:flex-none"
          :estudio-id="estudioId"
          @listo="visorListo"
          :imagenes="imagenes"
          :modalidad="estudio.modalidad"
          :paciente-nombre="estudio.pacienteNombre"
          :paciente-documento="estudio.pacienteDocumento"
          :hospital-nombre="estudio.hospitalNombre"
          :fecha-estudio="estudio.fechaEstudio"
        />

        <PanelEstudio
          class="max-xl:min-h-[26rem] xl:w-[400px] xl:flex-none"
          :estudio="estudio"
          :informes="informes"
          :imagenes="imagenes"
          @actualizar="actualizarEstudioEInformes"
        />
      </div>
    </template>

    <section v-else class="glass flex flex-1 items-center justify-center">
      <p class="text-ink-faint text-sm">{{ cargando ? 'Cargando estudio…' : 'Estudio no disponible.' }}</p>
    </section>
  </div>
</template>
