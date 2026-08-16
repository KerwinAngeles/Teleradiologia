<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch } from 'vue'

const props = defineProps<{ nombre: string }>()
const emit = defineEmits<{ cambio: [firma: string | null] }>()

const ANCHO = 640
const ALTO = 200

type Modo = 'trazar' | 'escribir'

const modo = ref<Modo>('trazar')
const lienzo = ref<HTMLCanvasElement | null>(null)
const tieneTrazo = ref(false)
const texto = ref(props.nombre)
const estiloElegido = ref<string | null>(null)
const fuentesListas = ref(false)

const estilos = [
  { id: 'Great Vibes', etiqueta: 'Cursiva clásica', tamano: 74, y: 0.66 },
  { id: 'Dancing Script', etiqueta: 'Manuscrita', tamano: 64, y: 0.64 },
  { id: 'Caveat', etiqueta: 'Rápida', tamano: 72, y: 0.64 },
]

let dibujando = false
let ultimo: { x: number; y: number } | null = null

function contexto(): CanvasRenderingContext2D | null {
  return lienzo.value?.getContext('2d') ?? null
}

function limpiarLienzo() {
  const ctx = contexto()
  if (!ctx || !lienzo.value) return
  ctx.clearRect(0, 0, lienzo.value.width, lienzo.value.height)
}

function tintaActual(): string {
  // El trazo sigue al tema: en modo oscuro una firma negra sería invisible.
  return getComputedStyle(document.documentElement).getPropertyValue('--color-ink').trim() || '#14131a'
}

function posicion(evento: PointerEvent) {
  const caja = lienzo.value!.getBoundingClientRect()
  return {
    x: ((evento.clientX - caja.left) / caja.width) * ANCHO,
    y: ((evento.clientY - caja.top) / caja.height) * ALTO,
  }
}

function empezar(evento: PointerEvent) {
  if (modo.value !== 'trazar') return
  lienzo.value?.setPointerCapture(evento.pointerId)
  dibujando = true
  ultimo = posicion(evento)
}

function mover(evento: PointerEvent) {
  if (!dibujando || !ultimo) return

  const ctx = contexto()
  if (!ctx) return

  const actual = posicion(evento)

  ctx.strokeStyle = tintaActual()
  ctx.lineWidth = 2.4
  ctx.lineCap = 'round'
  ctx.lineJoin = 'round'

  ctx.beginPath()
  ctx.moveTo(ultimo.x, ultimo.y)
  // Curva hacia el punto medio: sin esto el trazo se ve como una polilínea con esquinas.
  ctx.quadraticCurveTo(ultimo.x, ultimo.y, (ultimo.x + actual.x) / 2, (ultimo.y + actual.y) / 2)
  ctx.stroke()

  ultimo = actual
  tieneTrazo.value = true
}

function terminar() {
  if (!dibujando) return
  dibujando = false
  ultimo = null
  emitir()
}

function emitir() {
  if (!lienzo.value || !tieneTrazo.value) {
    emit('cambio', null)
    return
  }
  emit('cambio', lienzo.value.toDataURL('image/png'))
}

function limpiar() {
  limpiarLienzo()
  tieneTrazo.value = false
  estiloElegido.value = null
  emit('cambio', null)
}

function dibujarNombre(estilo: (typeof estilos)[number]) {
  const ctx = contexto()
  if (!ctx || !texto.value.trim()) return

  limpiarLienzo()

  ctx.fillStyle = tintaActual()
  ctx.textAlign = 'center'
  ctx.textBaseline = 'alphabetic'

  let tamano = estilo.tamano
  ctx.font = `${tamano}px "${estilo.id}", cursive`

  // Se achica hasta entrar: un nombre largo se saldría del recuadro.
  while (ctx.measureText(texto.value).width > ANCHO - 80 && tamano > 24) {
    tamano -= 2
    ctx.font = `${tamano}px "${estilo.id}", cursive`
  }

  ctx.fillText(texto.value.trim(), ANCHO / 2, ALTO * estilo.y)

  tieneTrazo.value = true
  estiloElegido.value = estilo.id
  emitir()
}

watch(modo, () => limpiar())
watch(texto, () => {
  if (estiloElegido.value) {
    const estilo = estilos.find((e) => e.id === estiloElegido.value)
    if (estilo) dibujarNombre(estilo)
  }
})

// El tema puede cambiar con la firma ya trazada: se redibuja para que no quede invisible.
let observador: MutationObserver | null = null

onMounted(async () => {
  try {
    await Promise.all(estilos.map((e) => document.fonts.load(`64px "${e.id}"`)))
    await document.fonts.ready
  } catch {
    // Sin las fuentes cargadas el navegador cae a la cursiva del sistema.
  }
  fuentesListas.value = true

  observador = new MutationObserver(() => {
    if (estiloElegido.value) {
      const estilo = estilos.find((e) => e.id === estiloElegido.value)
      if (estilo) dibujarNombre(estilo)
    }
  })
  observador.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] })
})

onBeforeUnmount(() => observador?.disconnect())
</script>

<template>
  <div class="space-y-4">
    <div class="flex flex-wrap gap-2">
      <button
        type="button"
        class="chip transition-colors"
        :class="modo === 'trazar' ? 'chip-informe' : 'chip-neutro'"
        @click="modo = 'trazar'"
      >
        Trazar con el mouse
      </button>
      <button
        type="button"
        class="chip transition-colors"
        :class="modo === 'escribir' ? 'chip-informe' : 'chip-neutro'"
        @click="modo = 'escribir'"
      >
        Generar desde mi nombre
      </button>
    </div>

    <div v-if="modo === 'escribir'" class="space-y-3">
      <div>
        <label class="meta-label mb-1.5 block" for="firma-nombre">Nombre completo</label>
        <input id="firma-nombre" v-model="texto" type="text" maxlength="60" class="field" />
      </div>

      <div class="grid grid-cols-1 gap-2 sm:grid-cols-3">
        <button
          v-for="estilo in estilos"
          :key="estilo.id"
          type="button"
          :disabled="!texto.trim()"
          class="rounded-2xl border px-3 py-3 text-center transition-colors disabled:opacity-40"
          :class="
            estiloElegido === estilo.id
              ? 'border-[var(--color-borde-fuerte)] bg-[var(--color-superficie-suave)]'
              : 'border-[var(--color-borde)] hover:bg-[var(--color-superficie-suave)]'
          "
          @click="dibujarNombre(estilo)"
        >
          <span
            class="block truncate text-2xl leading-tight"
            :style="{ fontFamily: `'${estilo.id}', cursive` }"
          >
            {{ texto.trim() || 'Tu nombre' }}
          </span>
          <span class="meta-label mt-1.5 block">{{ estilo.etiqueta }}</span>
        </button>
      </div>
    </div>

    <div class="relative">
      <canvas
        ref="lienzo"
        :width="ANCHO"
        :height="ALTO"
        class="w-full rounded-[1.1rem] border border-dashed bg-[var(--color-campo)]"
        :class="[
          modo === 'trazar' ? 'cursor-crosshair border-[var(--color-borde-fuerte)]' : 'border-[var(--color-borde)]',
        ]"
        @pointerdown="empezar"
        @pointermove="mover"
        @pointerup="terminar"
        @pointerleave="terminar"
      />

      <p
        v-if="!tieneTrazo"
        class="text-ink-faint pointer-events-none absolute inset-0 flex items-center justify-center text-sm"
      >
        {{ modo === 'trazar' ? 'Dibujá tu firma acá' : 'Elegí un estilo para generarla' }}
      </p>

      <div class="absolute right-3 bottom-3">
        <button
          v-if="tieneTrazo"
          type="button"
          class="btn-ghost !px-3 !py-1.5 !text-xs"
          @click="limpiar"
        >
          Limpiar
        </button>
      </div>
    </div>

    <p v-if="!fuentesListas && modo === 'escribir'" class="text-ink-faint text-xs">Cargando tipografías…</p>
  </div>
</template>
