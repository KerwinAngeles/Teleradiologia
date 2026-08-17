<script setup lang="ts">
import { ref, computed, shallowRef, onMounted, onBeforeUnmount, watch } from 'vue'
import { RenderingEngine, Enums, cache, type Types } from '@cornerstonejs/core'
import {
  ToolGroupManager,
  Enums as ToolsEnums,
  ArrowAnnotateTool,
  LengthTool,
  PanTool,
  StackScrollTool,
  WindowLevelTool,
  ZoomTool,
} from '@cornerstonejs/tools'
import { inicializarCornerstone, imageIdDeCorte } from '@/services/cornerstone'
import RailSeries from '@/components/visor/RailSeries.vue'
import type { ImagenEstudio } from '@/types/estudio'

const props = defineProps<{
  estudioId: string
  imagenes: ImagenEstudio[]
  modalidad: string
  pacienteNombre?: string
  pacienteDocumento?: string
  hospitalNombre?: string
  fechaEstudio?: string
}>()

// Se emite pase lo que pase —con imágenes, sin imágenes o con error—: quien esté
// esperando para tapar la pantalla necesita saber que el visor ya terminó.
const emit = defineEmits<{ listo: [] }>()

// Ids únicos por instancia: dos visores no deben pisarse el rendering engine.
const sufijo = Math.random().toString(36).slice(2, 10)
const renderingEngineId = `re-${sufijo}`
const viewportId = `vp-${sufijo}`
const toolGroupId = `tg-${sufijo}`

const contenedor = ref<HTMLDivElement | null>(null)
const raiz = ref<HTMLDivElement | null>(null)
const escena = ref<HTMLDivElement | null>(null)
// shallowRef: un proxy reactivo profundo rompe los objetos WebGL de Cornerstone.
const renderingEngine = shallowRef<RenderingEngine | null>(null)
const viewport = shallowRef<Types.IStackViewport | null>(null)

const cargando = ref(true)
const error = ref<string | null>(null)
const indice = ref(0)
const ventana = ref<{ ancho: number; centro: number } | null>(null)
const pantallaCompleta = ref(false)
const controlesVisibles = ref(true)

type Herramienta = 'ventana' | 'medir' | 'anotar' | 'zoom'

const HERRAMIENTAS: Record<Herramienta, string> = {
  ventana: WindowLevelTool.toolName,
  medir: LengthTool.toolName,
  anotar: ArrowAnnotateTool.toolName,
  zoom: ZoomTool.toolName,
}

const herramienta = ref<Herramienta>('ventana')

// Cada cuadro de un multi-frame es una imagen navegable: la pila mezcla cortes y cuadros.
const listaImageIds = computed(() =>
  props.imagenes.flatMap((img) =>
    img.numeroDeCuadros > 1
      ? Array.from({ length: img.numeroDeCuadros }, (_, c) =>
          imageIdDeCorte(props.estudioId, img.orthancInstanceId, c),
        )
      : [imageIdDeCorte(props.estudioId, img.orthancInstanceId)],
  ),
)

const totalCortes = computed(() => listaImageIds.value.length)

// Un cine es una sola instancia con muchos cuadros; una serie de cortes son muchas instancias.
const esCine = computed(() => props.imagenes.some((i) => i.numeroDeCuadros > 1))

// En qué posición de la pila empieza cada instancia: es lo que conecta el rail
// —que lista instancias— con el visor, que navega cuadros.
const inicioDeInstancia = computed(() => {
  const inicios: number[] = []
  let acumulado = 0
  for (const imagen of props.imagenes) {
    inicios.push(acumulado)
    acumulado += imagen.numeroDeCuadros > 1 ? imagen.numeroDeCuadros : 1
  }
  return inicios
})

const instanciaActiva = computed(() => {
  const inicios = inicioDeInstancia.value
  for (let i = inicios.length - 1; i >= 0; i--) {
    if (indice.value >= inicios[i]) return i
  }
  return 0
})

const zoom = ref(1)
const reproduciendo = ref(false)
const cuadrosPorSegundo = ref(15)
let temporizadorCine: ReturnType<typeof setInterval> | null = null

function alternarCine() {
  reproduciendo.value ? detenerCine() : iniciarCine()
}

function iniciarCine() {
  if (totalCortes.value < 2) return
  detenerCine()
  reproduciendo.value = true
  temporizadorCine = setInterval(() => {
    // Da la vuelta al llegar al final: un loop de ecografía se mira en bucle.
    const siguiente = (indice.value + 1) % totalCortes.value
    void irACorte(siguiente)
  }, 1000 / cuadrosPorSegundo.value)
}

function detenerCine() {
  if (temporizadorCine) clearInterval(temporizadorCine)
  temporizadorCine = null
  reproduciendo.value = false
}

watch(cuadrosPorSegundo, () => {
  if (reproduciendo.value) iniciarCine()
})

const esTC = computed(() => props.modalidad === 'CT')

// Presets de ventana en unidades Hounsfield: solo aplican a TC.
const PRESETS = [
  { nombre: 'Cerebro', ancho: 80, centro: 40 },
  { nombre: 'Hueso', ancho: 2000, centro: 400 },
  { nombre: 'Pulmón', ancho: 1500, centro: -600 },
  { nombre: 'Abdomen', ancho: 400, centro: 50 },
]

const presets = computed(() => (esTC.value ? PRESETS : []))

// Los topes de los deslizadores se fijan con la primera ventana que da la imagen
// y no se recalculan: si siguieran al valor, el pulgar se movería solo al arrastrar.
const limites = ref<{ anchoMax: number; centroMin: number; centroMax: number } | null>(null)

function capturarLimites() {
  if (limites.value || !ventana.value) return
  const { ancho, centro } = ventana.value
  const anchoMax = Math.max(1024, Math.round(ancho * 3))
  limites.value = {
    anchoMax,
    centroMin: Math.round(centro - anchoMax),
    centroMax: Math.round(centro + anchoMax),
  }
}

function alCambiarCorte() {
  const vp = viewport.value
  if (!vp) return
  indice.value = vp.getCurrentImageIdIndex()
  zoom.value = vp.getZoom()
}

function alCambiarVentana() {
  const rango = viewport.value?.getProperties().voiRange
  if (!rango) return
  ventana.value = {
    ancho: Math.round(rango.upper - rango.lower),
    centro: Math.round((rango.upper + rango.lower) / 2),
  }
  capturarLimites()
}

async function irACorte(i: number) {
  const vp = viewport.value
  if (!vp || i < 0 || i >= totalCortes.value) return
  await vp.setImageIdIndex(i)
}

function seleccionarInstancia(i: number) {
  detenerCine()
  void irACorte(inicioDeInstancia.value[i] ?? 0)
}

function aplicarVentana(ancho: number, centro: number) {
  const vp = viewport.value
  if (!vp) return
  vp.setProperties({ voiRange: { lower: centro - ancho / 2, upper: centro + ancho / 2 } })
  vp.render()
  alCambiarVentana()
}

function reencuadrar() {
  const vp = viewport.value
  if (!vp) return
  vp.resetCamera()
  vp.resetProperties()
  vp.render()
  limites.value = null
  alCambiarVentana()
}

function cambiarHerramienta(nueva: Herramienta) {
  const grupo = ToolGroupManager.getToolGroup(toolGroupId)
  if (!grupo) return

  herramienta.value = nueva
  for (const nombre of Object.values(HERRAMIENTAS)) grupo.setToolPassive(nombre)

  const primario = { mouseButton: ToolsEnums.MouseBindings.Primary }
  const secundario = { mouseButton: ToolsEnums.MouseBindings.Secondary }

  if (nueva === 'zoom') {
    grupo.setToolActive(ZoomTool.toolName, { bindings: [primario, secundario] })
    return
  }

  grupo.setToolActive(HERRAMIENTAS[nueva], { bindings: [primario] })
  // El zoom con botón derecho no depende de la herramienta elegida.
  grupo.setToolActive(ZoomTool.toolName, { bindings: [secundario] })
}

// El canvas de Cornerstone no sigue al contenedor por sí solo: hay que reajustarlo a mano.
let observadorTamano: ResizeObserver | null = null
let cuadroReajuste = 0
// Un resize normal conserva la cámara; al entrar o salir de pantalla completa se reencuadra
// para que la imagen aproveche el espacio nuevo en lugar de quedar recortada.
let reencuadrarPendiente = false

function reajustarViewport() {
  cancelAnimationFrame(cuadroReajuste)
  cuadroReajuste = requestAnimationFrame(() => {
    const conservarCamara = !reencuadrarPendiente
    reencuadrarPendiente = false
    renderingEngine.value?.resize(true, conservarCamara)
    if (viewport.value) zoom.value = viewport.value.getZoom()
  })
}

async function alternarPantallaCompleta() {
  if (pantallaCompleta.value) {
    if (document.fullscreenElement) await document.exitFullscreen().catch(() => {})
    else pantallaCompleta.value = false
    return
  }

  try {
    await raiz.value?.requestFullscreen()
  } catch {
    // Sin API nativa (o denegada) el overlay fijo cubre igual toda la ventana.
    pantallaCompleta.value = true
  }
}

function alCambiarFullscreen() {
  if (document.fullscreenElement === raiz.value) pantallaCompleta.value = true
  else if (pantallaCompleta.value && document.fullscreenElement === null) pantallaCompleta.value = false
}

watch(pantallaCompleta, (activa) => {
  document.body.classList.toggle('overflow-hidden', activa)
  if (activa) contenedor.value?.focus()
  reencuadrarPendiente = true
  reajustarViewport()
})

function onKeydown(evento: KeyboardEvent) {
  if (evento.key === 'ArrowLeft') void irACorte(indice.value - 1)
  if (evento.key === 'ArrowRight') void irACorte(indice.value + 1)
  if (evento.key === 'f' || evento.key === 'F') void alternarPantallaCompleta()
  if (evento.key === ' ' && esCine.value) {
    evento.preventDefault()
    alternarCine()
  }
}

// En pantalla completa el foco puede estar fuera del canvas: se escucha a nivel documento.
function onKeydownGlobal(evento: KeyboardEvent) {
  if (!pantallaCompleta.value) return
  if (evento.target instanceof HTMLElement && ['INPUT', 'TEXTAREA'].includes(evento.target.tagName)) return
  if (evento.key === 'Escape' && !document.fullscreenElement) {
    pantallaCompleta.value = false
    return
  }
  if (evento.target !== contenedor.value) onKeydown(evento)
}

async function montarVisor() {
  try {
    const elemento = contenedor.value
    if (!elemento || props.imagenes.length === 0) return

    await inicializarCornerstone()

    const motor = new RenderingEngine(renderingEngineId)
    renderingEngine.value = motor
    motor.enableElement({ viewportId, type: Enums.ViewportType.STACK, element: elemento })

    const vp = motor.getViewport(viewportId) as Types.IStackViewport
    viewport.value = vp

    const grupo = ToolGroupManager.createToolGroup(toolGroupId)
    if (grupo) {
      grupo.addTool(WindowLevelTool.toolName)
      grupo.addTool(PanTool.toolName)
      grupo.addTool(ZoomTool.toolName)
      grupo.addTool(StackScrollTool.toolName)
      grupo.addTool(LengthTool.toolName)
      grupo.addTool(ArrowAnnotateTool.toolName)

      grupo.setToolActive(WindowLevelTool.toolName, {
        bindings: [{ mouseButton: ToolsEnums.MouseBindings.Primary }],
      })
      grupo.setToolActive(ZoomTool.toolName, {
        bindings: [{ mouseButton: ToolsEnums.MouseBindings.Secondary }],
      })
      grupo.setToolActive(PanTool.toolName, {
        bindings: [{ mouseButton: ToolsEnums.MouseBindings.Auxiliary }],
      })
      grupo.setToolActive(StackScrollTool.toolName, {
        bindings: [{ mouseButton: ToolsEnums.MouseBindings.Wheel }],
      })
      grupo.addViewport(viewportId, renderingEngineId)
    }

    elemento.addEventListener(Enums.Events.STACK_NEW_IMAGE, alCambiarCorte)
    elemento.addEventListener(Enums.Events.VOI_MODIFIED, alCambiarVentana)

    if (escena.value) {
      observadorTamano = new ResizeObserver(() => reajustarViewport())
      observadorTamano.observe(escena.value)
    }
    document.addEventListener('fullscreenchange', alCambiarFullscreen)
    document.addEventListener('keydown', onKeydownGlobal)

    await vp.setStack(listaImageIds.value, 0)
    vp.render()
    alCambiarVentana()
  } catch {
    error.value = 'No se pudo inicializar el visor.'
  } finally {
    cargando.value = false
    emit('listo')
  }
}

function desmontarVisor() {
  detenerCine()
  contenedor.value?.removeEventListener(Enums.Events.STACK_NEW_IMAGE, alCambiarCorte)
  contenedor.value?.removeEventListener(Enums.Events.VOI_MODIFIED, alCambiarVentana)

  cancelAnimationFrame(cuadroReajuste)
  observadorTamano?.disconnect()
  observadorTamano = null
  document.removeEventListener('fullscreenchange', alCambiarFullscreen)
  document.removeEventListener('keydown', onKeydownGlobal)
  document.body.classList.remove('overflow-hidden')
  if (document.fullscreenElement === raiz.value) void document.exitFullscreen().catch(() => {})

  ToolGroupManager.destroyToolGroup(toolGroupId)
  renderingEngine.value?.destroy()
  renderingEngine.value = null
  viewport.value = null

  // Una serie larga ocupa cientos de MB decodificados.
  cache.purgeCache()
}

onMounted(montarVisor)
onBeforeUnmount(desmontarVisor)

watch(
  () => props.imagenes,
  async (nuevas) => {
    if (nuevas.length === 0 || !viewport.value) return
    detenerCine()
    limites.value = null
    await viewport.value.setStack(listaImageIds.value, 0)
    viewport.value.render()
    alCambiarVentana()
  },
)
</script>

<template>
  <div
    ref="raiz"
    class="visor-escenario flex overflow-hidden"
    :class="pantallaCompleta ? 'fixed inset-0 z-[70]' : 'min-h-0 flex-1 rounded-[1.1rem]'"
  >
    <RailSeries
      v-if="totalCortes > 0 && !error"
      class="border-r border-white/10 p-3"
      :estudio-id="estudioId"
      :imagenes="imagenes"
      :instancia-activa="instanciaActiva"
      :presets="presets"
      @seleccionar="seleccionarInstancia"
      @preset="aplicarVentana"
    />

    <div class="flex min-h-0 min-w-0 flex-1 flex-col">
      <!-- Cabecera del escenario: identificación técnica a la izquierda, lectura a la derecha. -->
      <div class="flex flex-none items-center gap-4 border-b border-white/10 px-4 py-2">
        <p class="visor-kicker">Visor · {{ modalidad }}</p>

        <div class="visor-lectura ml-auto flex flex-wrap items-center gap-x-4 gap-y-1">
          <span v-if="ventana">W {{ ventana.ancho }}</span>
          <span v-if="ventana">L {{ ventana.centro }}</span>
          <span>Zoom {{ (zoom * 100).toFixed(0) }}%</span>
          <span v-if="totalCortes">{{ esCine ? 'Cuadro' : 'Corte' }} {{ indice + 1 }}/{{ totalCortes }}</span>
        </div>

        <button
          v-if="pantallaCompleta"
          type="button"
          class="visor-tool !text-[0.6875rem]"
          @click="controlesVisibles = !controlesVisibles"
        >
          {{ controlesVisibles ? 'Ocultar controles' : 'Mostrar controles' }}
        </button>

        <button
          type="button"
          class="visor-tool !h-7 !w-7 !justify-center !rounded-full !px-0"
          :title="pantallaCompleta ? 'Salir de pantalla completa (Esc)' : 'Pantalla completa (F)'"
          @click="alternarPantallaCompleta"
        >
          <svg
            v-if="pantallaCompleta"
            class="h-3.5 w-3.5"
            fill="none"
            viewBox="0 0 24 24"
            stroke-width="1.6"
            stroke="currentColor"
          >
            <path stroke-linecap="round" stroke-linejoin="round" d="M9 9V4.5M9 9H4.5M9 9 3.75 3.75M9 15v4.5M9 15H4.5M9 15l-5.25 5.25M15 9h4.5M15 9V4.5M15 9l5.25-5.25M15 15h4.5M15 15v4.5m0-4.5 5.25 5.25" />
          </svg>
          <svg v-else class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke-width="1.6" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 3.75v4.5m0-4.5h4.5m-4.5 0L9 9M3.75 20.25v-4.5m0 4.5h4.5m-4.5 0L9 15M20.25 3.75h-4.5m4.5 0v4.5m0-4.5L15 9m5.25 11.25h-4.5m4.5 0v-4.5m0 4.5L15 15" />
          </svg>
        </button>
      </div>

      <!-- Escenario -->
      <div ref="escena" class="relative min-h-0 flex-1 overflow-hidden">
        <div
          ref="contenedor"
          class="h-full w-full outline-none"
          tabindex="0"
          @contextmenu.prevent
          @keydown="onKeydown"
        />

        <!-- Overlay DICOM: no intercepta el mouse, para no robarle los gestos al visor. -->
        <div
          v-if="!cargando && totalCortes > 0 && !error"
          class="pointer-events-none absolute inset-0 p-4 font-mono text-[0.6875rem] leading-snug text-white/70"
        >
          <div class="absolute top-4 left-4">
            <p class="text-white/90">{{ pacienteNombre ?? '—' }}</p>
            <p v-if="pacienteDocumento">{{ pacienteDocumento }}</p>
          </div>

          <div class="absolute top-4 right-4 text-right">
            <p class="text-white/90">{{ hospitalNombre ?? '' }}</p>
            <p v-if="fechaEstudio">{{ new Date(fechaEstudio).toLocaleDateString('es-AR') }}</p>
            <p>{{ modalidad }}</p>
          </div>

          <div class="absolute bottom-4 left-4">
            <p>{{ esCine ? 'Cuadro' : 'Corte' }} {{ indice + 1 }}/{{ totalCortes }}</p>
            <p v-if="ventana">W {{ ventana.ancho }} · L {{ ventana.centro }}</p>
          </div>

          <div class="absolute right-4 bottom-4 text-right">
            <p class="text-white/40">No apto para diagnóstico</p>
          </div>

          <!-- Marcadores de lateralidad: informar el lado equivocado es de los errores más graves. -->
          <div class="absolute top-1/2 left-4 -translate-y-1/2 text-sm font-semibold text-white/50">R</div>
          <div class="absolute top-1/2 right-4 -translate-y-1/2 text-sm font-semibold text-white/50">L</div>
        </div>

        <div v-if="cargando" class="absolute inset-0 flex items-center justify-center">
          <p class="text-sm text-white/40">Inicializando visor…</p>
        </div>
        <div v-else-if="totalCortes === 0" class="absolute inset-0 flex items-center justify-center">
          <p class="text-sm text-white/40">Este estudio todavía no tiene imágenes disponibles.</p>
        </div>
        <div v-else-if="error" class="absolute inset-0 flex items-center justify-center">
          <p class="text-sm text-red-300">{{ error }}</p>
        </div>
      </div>

      <!-- Toolbar -->
      <div
        v-if="totalCortes > 0 && !error"
        v-show="!pantallaCompleta || controlesVisibles"
        class="flex flex-none flex-col gap-3 border-t border-white/10 px-4 py-3"
      >
        <div class="flex flex-wrap items-center gap-x-5 gap-y-2">
          <div class="flex flex-wrap gap-1.5">
            <button
              type="button"
              class="visor-tool"
              :class="herramienta === 'ventana' && 'visor-tool-activa'"
              @click="cambiarHerramienta('ventana')"
            >
              Ventana
            </button>
            <button
              type="button"
              class="visor-tool"
              :class="herramienta === 'medir' && 'visor-tool-activa'"
              @click="cambiarHerramienta('medir')"
            >
              Medir
            </button>
            <button
              type="button"
              class="visor-tool"
              :class="herramienta === 'anotar' && 'visor-tool-activa'"
              @click="cambiarHerramienta('anotar')"
            >
              Anotar
            </button>
            <button
              type="button"
              class="visor-tool"
              :class="herramienta === 'zoom' && 'visor-tool-activa'"
              @click="cambiarHerramienta('zoom')"
            >
              Zoom
            </button>
          </div>

          <div v-if="ventana && limites" class="flex flex-wrap items-center gap-x-4 gap-y-2 border-l border-white/10 pl-5">
            <label class="visor-lectura flex items-center gap-2">
              W
              <input
                type="range"
                min="1"
                :max="limites.anchoMax"
                :value="ventana.ancho"
                class="w-24 accent-[var(--color-lilac)]"
                aria-label="Ancho de ventana"
                @input="aplicarVentana(Number(($event.target as HTMLInputElement).value), ventana.centro)"
              />
              <span class="w-10 !text-white/80 tabular-nums">{{ ventana.ancho }}</span>
            </label>
            <label class="visor-lectura flex items-center gap-2">
              L
              <input
                type="range"
                :min="limites.centroMin"
                :max="limites.centroMax"
                :value="ventana.centro"
                class="w-24 accent-[var(--color-lilac)]"
                aria-label="Centro de ventana"
                @input="aplicarVentana(ventana.ancho, Number(($event.target as HTMLInputElement).value))"
              />
              <span class="w-10 !text-white/80 tabular-nums">{{ ventana.centro }}</span>
            </label>
          </div>

          <button type="button" class="visor-tool ml-auto" @click="reencuadrar">Reencuadrar</button>
        </div>

        <div v-if="esCine" class="flex flex-wrap items-center gap-3 border-t border-white/10 pt-3">
          <button
            type="button"
            class="visor-tool !h-8 !w-8 !justify-center !rounded-full !px-0"
            :title="reproduciendo ? 'Pausar' : 'Reproducir'"
            @click="alternarCine"
          >
            <svg v-if="reproduciendo" class="h-3.5 w-3.5" viewBox="0 0 24 24" fill="currentColor">
              <path d="M6 5h4v14H6zM14 5h4v14h-4z" />
            </svg>
            <svg v-else class="ml-0.5 h-3.5 w-3.5" viewBox="0 0 24 24" fill="currentColor">
              <path d="M8 5v14l11-7z" />
            </svg>
          </button>

          <label class="visor-lectura flex items-center gap-2">
            Velocidad
            <input
              v-model.number="cuadrosPorSegundo"
              type="range"
              min="1"
              max="30"
              class="w-24 accent-[var(--color-lilac)]"
            />
            <span class="w-12 !text-white/80 tabular-nums">{{ cuadrosPorSegundo }} fps</span>
          </label>

          <span class="visor-lectura">Cine de {{ totalCortes }} cuadros</span>
        </div>

        <div v-if="totalCortes > 1" class="flex items-center gap-3">
          <button
            type="button"
            class="visor-tool"
            :disabled="indice === 0"
            @click="irACorte(indice - 1)"
          >
            ← Anterior
          </button>
          <input
            :value="indice"
            type="range"
            min="0"
            :max="totalCortes - 1"
            class="h-1 flex-1 accent-[var(--color-lilac)]"
            aria-label="Navegar cortes"
            @input="irACorte(Number(($event.target as HTMLInputElement).value))"
          />
          <button
            type="button"
            class="visor-tool"
            :disabled="indice === totalCortes - 1"
            @click="irACorte(indice + 1)"
          >
            Siguiente →
          </button>
        </div>

        <p class="visor-lectura">
          Arrastrar = herramienta activa · rueda = cortes · botón derecho = zoom · botón central = desplazar · F =
          pantalla completa
        </p>
      </div>
    </div>
  </div>
</template>
