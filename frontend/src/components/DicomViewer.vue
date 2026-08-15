<script setup lang="ts">
import { ref, computed, shallowRef, onMounted, onBeforeUnmount, watch } from 'vue'
import { RenderingEngine, Enums, cache, type Types } from '@cornerstonejs/core'
import {
  ToolGroupManager,
  Enums as ToolsEnums,
  LengthTool,
  PanTool,
  StackScrollTool,
  WindowLevelTool,
  ZoomTool,
} from '@cornerstonejs/tools'
import { inicializarCornerstone, imageIdDeCorte } from '@/services/cornerstone'
import type { ImagenEstudio } from '@/types/estudio'

const props = defineProps<{
  estudioId: string
  imagenes: ImagenEstudio[]
  modalidad: string
}>()

// Ids únicos por instancia: dos visores no deben pisarse el rendering engine.
const sufijo = Math.random().toString(36).slice(2, 10)
const renderingEngineId = `re-${sufijo}`
const viewportId = `vp-${sufijo}`
const toolGroupId = `tg-${sufijo}`

const contenedor = ref<HTMLDivElement | null>(null)
// shallowRef: un proxy reactivo profundo rompe los objetos WebGL de Cornerstone.
const renderingEngine = shallowRef<RenderingEngine | null>(null)
const viewport = shallowRef<Types.IStackViewport | null>(null)

const cargando = ref(true)
const error = ref<string | null>(null)
const indice = ref(0)
const ventana = ref<{ ancho: number; centro: number } | null>(null)
const modo = ref<'ventana' | 'medir'>('ventana')

const totalCortes = computed(() => props.imagenes.length)
const esTC = computed(() => props.modalidad === 'CT')

// Presets de ventana en unidades Hounsfield: solo aplican a TC.
const presets = [
  { nombre: 'Cerebro', ancho: 80, centro: 40 },
  { nombre: 'Hueso', ancho: 2000, centro: 400 },
  { nombre: 'Pulmón', ancho: 1500, centro: -600 },
  { nombre: 'Abdomen', ancho: 400, centro: 50 },
]

function alCambiarCorte() {
  const vp = viewport.value
  if (vp) indice.value = vp.getCurrentImageIdIndex()
}

function alCambiarVentana() {
  const rango = viewport.value?.getProperties().voiRange
  if (!rango) return
  ventana.value = {
    ancho: Math.round(rango.upper - rango.lower),
    centro: Math.round((rango.upper + rango.lower) / 2),
  }
}

async function irACorte(i: number) {
  const vp = viewport.value
  if (!vp || i < 0 || i >= totalCortes.value) return
  await vp.setImageIdIndex(i)
}

function aplicarPreset(ancho: number, centro: number) {
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
  alCambiarVentana()
}

function cambiarModo(nuevo: 'ventana' | 'medir') {
  const grupo = ToolGroupManager.getToolGroup(toolGroupId)
  if (!grupo) return

  modo.value = nuevo
  const primario = [{ mouseButton: ToolsEnums.MouseBindings.Primary }]

  if (nuevo === 'medir') {
    grupo.setToolPassive(WindowLevelTool.toolName)
    grupo.setToolActive(LengthTool.toolName, { bindings: primario })
  } else {
    grupo.setToolPassive(LengthTool.toolName)
    grupo.setToolActive(WindowLevelTool.toolName, { bindings: primario })
  }
}

function onKeydown(evento: KeyboardEvent) {
  if (evento.key === 'ArrowLeft') void irACorte(indice.value - 1)
  if (evento.key === 'ArrowRight') void irACorte(indice.value + 1)
}

async function montarVisor() {
  const elemento = contenedor.value
  if (!elemento || props.imagenes.length === 0) {
    cargando.value = false
    return
  }

  try {
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

    const imageIds = props.imagenes.map((img) => imageIdDeCorte(props.estudioId, img.orthancInstanceId))
    await vp.setStack(imageIds, 0)
    vp.render()
    alCambiarVentana()
  } catch {
    error.value = 'No se pudo inicializar el visor.'
  } finally {
    cargando.value = false
  }
}

function desmontarVisor() {
  contenedor.value?.removeEventListener(Enums.Events.STACK_NEW_IMAGE, alCambiarCorte)
  contenedor.value?.removeEventListener(Enums.Events.VOI_MODIFIED, alCambiarVentana)

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
    const imageIds = nuevas.map((img) => imageIdDeCorte(props.estudioId, img.orthancInstanceId))
    await viewport.value.setStack(imageIds, 0)
    viewport.value.render()
    alCambiarVentana()
  },
)
</script>

<template>
  <div class="glass overflow-hidden p-3">
    <div class="flex flex-wrap items-center justify-between gap-3 px-2 py-2">
      <p class="meta-label">Visor · {{ modalidad }}</p>
      <div class="flex items-center gap-3">
        <p v-if="ventana" class="meta-label tabular-nums">A {{ ventana.ancho }} · C {{ ventana.centro }}</p>
        <p v-if="totalCortes" class="meta-label tabular-nums">Corte {{ indice + 1 }} / {{ totalCortes }}</p>
      </div>
    </div>

    <div class="relative overflow-hidden rounded-[1.1rem] bg-[#0b0b11]">
      <div
        ref="contenedor"
        class="h-[58vh] min-h-[380px] w-full outline-none"
        tabindex="0"
        @contextmenu.prevent
        @keydown="onKeydown"
      />

      <div v-if="cargando" class="absolute inset-0 flex items-center justify-center">
        <p class="text-sm text-white/40">Inicializando visor…</p>
      </div>
      <div v-else-if="totalCortes === 0" class="absolute inset-0 flex items-center justify-center">
        <p class="text-sm text-white/40">Este estudio todavía no tiene imágenes disponibles.</p>
      </div>
      <div v-else-if="error" class="absolute inset-0 flex items-center justify-center">
        <p class="text-sm text-red-300">{{ error }}</p>
      </div>

      <div v-if="totalCortes > 0 && !error" class="space-y-3 border-t border-white/10 px-4 py-3">
        <div class="flex flex-wrap items-center gap-2">
          <button
            type="button"
            class="rounded-full border px-3 py-1.5 text-xs font-medium transition-colors"
            :class="
              modo === 'ventana'
                ? 'border-white/70 bg-white text-[#0b0b11]'
                : 'border-white/15 bg-white/10 text-white hover:bg-white/20'
            "
            @click="cambiarModo('ventana')"
          >
            Ventana
          </button>
          <button
            type="button"
            class="rounded-full border px-3 py-1.5 text-xs font-medium transition-colors"
            :class="
              modo === 'medir'
                ? 'border-white/70 bg-white text-[#0b0b11]'
                : 'border-white/15 bg-white/10 text-white hover:bg-white/20'
            "
            @click="cambiarModo('medir')"
          >
            Medir
          </button>

          <span class="mx-1 h-4 w-px bg-white/15" />

          <button
            v-for="preset in esTC ? presets : []"
            :key="preset.nombre"
            type="button"
            class="rounded-full border border-white/15 bg-white/10 px-3 py-1.5 text-xs font-medium text-white transition-colors hover:bg-white/20"
            @click="aplicarPreset(preset.ancho, preset.centro)"
          >
            {{ preset.nombre }}
          </button>

          <button
            type="button"
            class="ml-auto rounded-full border border-white/15 bg-white/10 px-3 py-1.5 text-xs font-medium text-white transition-colors hover:bg-white/20"
            @click="reencuadrar"
          >
            Reencuadrar
          </button>
        </div>

        <div v-if="totalCortes > 1" class="flex items-center gap-4">
          <button
            type="button"
            class="rounded-full border border-white/15 bg-white/10 px-3 py-1.5 text-xs font-medium text-white transition-colors hover:bg-white/20 disabled:opacity-30"
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
            class="h-1 flex-1 accent-white"
            aria-label="Navegar cortes"
            @input="irACorte(Number(($event.target as HTMLInputElement).value))"
          />
          <button
            type="button"
            class="rounded-full border border-white/15 bg-white/10 px-3 py-1.5 text-xs font-medium text-white transition-colors hover:bg-white/20 disabled:opacity-30"
            :disabled="indice === totalCortes - 1"
            @click="irACorte(indice + 1)"
          >
            Siguiente →
          </button>
        </div>

        <p class="text-[0.6875rem] tracking-[0.08em] text-white/35 uppercase">
          Arrastrar = ventana · rueda = cortes · botón derecho = zoom · botón central = desplazar
        </p>
      </div>
    </div>
  </div>
</template>
