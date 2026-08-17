<script setup lang="ts">
import { computed, ref } from 'vue'
import type { Segmento } from '@/composables/useEstadisticas'

const props = defineProps<{ datos: Segmento[]; unidad?: string }>()

const RADIO = 60
const GROSOR = 18
const CENTRO = 70
const SEPARACION = 2.5

const total = computed(() => props.datos.reduce((s, d) => s + d.valor, 0))
const conValor = computed(() => props.datos.filter((d) => d.valor > 0))
const resaltado = ref<string | null>(null)

interface Arco extends Segmento {
  d: string
  porcentaje: number
}

function punto(angulo: number, radio: number) {
  const rad = ((angulo - 90) * Math.PI) / 180
  return [CENTRO + radio * Math.cos(rad), CENTRO + radio * Math.sin(rad)]
}

function anillo(desde: number, hasta: number, expandir: number): string {
  const rExterno = RADIO + expandir
  const rInterno = RADIO - GROSOR
  const [x1, y1] = punto(desde, rExterno)
  const [x2, y2] = punto(hasta, rExterno)
  const [x3, y3] = punto(hasta, rInterno)
  const [x4, y4] = punto(desde, rInterno)
  const mayor = hasta - desde > 180 ? 1 : 0

  return [
    `M ${x1} ${y1}`,
    `A ${rExterno} ${rExterno} 0 ${mayor} 1 ${x2} ${y2}`,
    `L ${x3} ${y3}`,
    `A ${rInterno} ${rInterno} 0 ${mayor} 0 ${x4} ${y4}`,
    'Z',
  ].join(' ')
}

function porcentajeDe(valor: number) {
  return total.value === 0 ? 0 : Math.round((valor / total.value) * 100)
}

const arcos = computed<Arco[]>(() => {
  if (total.value === 0) return []

  // Con un solo segmento la separación dejaría un anillo cortado sin motivo.
  const separacion = conValor.value.length > 1 ? SEPARACION : 0
  let angulo = 0

  return conValor.value.map((segmento) => {
    const barrido = (segmento.valor / total.value) * 360
    const desde = angulo + separacion / 2
    const hasta = angulo + barrido - separacion / 2
    angulo += barrido
    // El segmento apuntado crece 3px hacia afuera: se distingue sin depender del color.
    const expandir = resaltado.value === segmento.clave ? 3 : 0
    return {
      ...segmento,
      d: anillo(desde, Math.max(hasta, desde + 0.1), expandir),
      porcentaje: porcentajeDe(segmento.valor),
    }
  })
})

// El centro es el lugar de lectura: en reposo muestra el total y al apuntar un
// segmento muestra ese segmento, sin obligar a cruzar la vista con la leyenda.
const foco = computed(() => {
  const activo = props.datos.find((d) => d.clave === resaltado.value)
  if (!activo) return { valor: total.value, etiqueta: props.unidad ?? 'estudios' }
  return { valor: activo.valor, etiqueta: `${activo.etiqueta} · ${porcentajeDe(activo.valor)}%` }
})

const descripcion = computed(() =>
  props.datos.map((d) => `${d.etiqueta}: ${d.valor} (${porcentajeDe(d.valor)}%)`).join(', '),
)
</script>

<template>
  <div v-if="total === 0" class="text-ink-faint flex h-full min-h-[180px] items-center justify-center text-sm">
    Todavía no hay datos.
  </div>

  <div v-else class="flex flex-wrap items-center justify-center gap-6 sm:flex-nowrap sm:justify-start">
    <svg
      :viewBox="`0 0 ${CENTRO * 2} ${CENTRO * 2}`"
      class="h-[150px] w-[150px] flex-none overflow-visible"
      role="img"
      :aria-label="descripcion"
    >
      <path
        v-for="arco in arcos"
        :key="arco.clave"
        :d="arco.d"
        :fill="arco.color"
        :opacity="resaltado === null || resaltado === arco.clave ? 1 : 0.32"
        class="transition-[opacity,d] duration-200"
        @mouseenter="resaltado = arco.clave"
        @mouseleave="resaltado = null"
      />
      <text
        :x="CENTRO"
        :y="CENTRO - 2"
        text-anchor="middle"
        class="fill-[var(--color-ink)] text-[1.5rem] font-light tabular-nums"
      >
        {{ foco.valor }}
      </text>
      <text
        :x="CENTRO"
        :y="CENTRO + 14"
        text-anchor="middle"
        class="fill-[var(--color-ink-faint)] text-[0.5rem] tracking-[0.1em] uppercase"
      >
        {{ foco.etiqueta }}
      </text>
    </svg>

    <ul class="min-w-[9rem] flex-1 space-y-1">
      <li v-for="segmento in datos" :key="segmento.clave">
        <button
          type="button"
          class="flex w-full items-center gap-2.5 rounded-lg px-2 py-1.5 text-left outline-none transition-colors hover:bg-white/50 focus-visible:bg-white/50"
          @mouseenter="resaltado = segmento.clave"
          @mouseleave="resaltado = null"
          @focus="resaltado = segmento.clave"
          @blur="resaltado = null"
        >
          <span class="h-2.5 w-2.5 flex-none rounded-full" :style="{ background: segmento.color }" />
          <span class="flex-1 text-xs font-medium">{{ segmento.etiqueta }}</span>
          <span class="text-ink-soft text-xs tabular-nums">{{ segmento.valor }}</span>
          <span class="text-ink-faint w-9 text-right text-xs tabular-nums">{{ porcentajeDe(segmento.valor) }}%</span>
        </button>
      </li>
    </ul>
  </div>
</template>
