<script setup lang="ts">
import { computed, ref } from 'vue'
import type { PuntoTendencia, Serie } from '@/composables/useEstadisticas'

const props = defineProps<{ datos: PuntoTendencia[]; series: Serie[] }>()

const ANCHO = 640
const ALTO = 170
const MARGEN_SUP = 12
const MARGEN_INF = 6

const indiceActivo = ref<number | null>(null)

const maximo = computed(() => Math.max(1, ...props.datos.flatMap((d) => [d.entradas, d.salidas])))

// El eje arranca en cero: recortarlo exagera diferencias que no existen.
const marcas = computed(() => {
  const paso = Math.max(1, Math.ceil(maximo.value / 3))
  const valores: number[] = []
  for (let v = 0; v <= maximo.value; v += paso) valores.push(v)
  if (valores[valores.length - 1] !== maximo.value) valores.push(maximo.value)
  return valores
})

function proporcionDe(indice: number) {
  return props.datos.length <= 1 ? 0.5 : indice / (props.datos.length - 1)
}

function x(indice: number) {
  return proporcionDe(indice) * ANCHO
}

function y(valor: number) {
  const util = ALTO - MARGEN_SUP - MARGEN_INF
  return ALTO - MARGEN_INF - (valor / maximo.value) * util
}

function linea(clave: 'entradas' | 'salidas') {
  return props.datos.map((d, i) => `${i === 0 ? 'M' : 'L'} ${x(i)} ${y(d[clave])}`).join(' ')
}

function area(clave: 'entradas' | 'salidas') {
  if (props.datos.length === 0) return ''
  return `${linea(clave)} L ${x(props.datos.length - 1)} ${ALTO - MARGEN_INF} L ${x(0)} ${ALTO - MARGEN_INF} Z`
}

const activo = computed(() => (indiceActivo.value === null ? null : props.datos[indiceActivo.value]))

// El tooltip se ancla al día apuntado y se corre al lado contrario cerca del borde.
const posicionTooltip = computed(() => {
  if (indiceActivo.value === null) return { left: '0%', transform: 'translateX(-50%)' }
  const proporcion = proporcionDe(indiceActivo.value)
  const transform = proporcion < 0.15 ? 'none' : proporcion > 0.85 ? 'translateX(-100%)' : 'translateX(-50%)'
  return { left: `${proporcion * 100}%`, transform }
})

const descripcion = computed(() => {
  const [entradas, salidas] = props.series
  return props.datos
    .map((d) => `${d.etiqueta}: ${entradas.etiqueta} ${d.entradas}, ${salidas.etiqueta} ${d.salidas}`)
    .join('. ')
})

const sinDatos = computed(() => props.datos.every((d) => d.entradas === 0 && d.salidas === 0))
</script>

<template>
  <div v-if="sinDatos" class="text-ink-faint flex h-full min-h-[170px] items-center justify-center text-sm">
    Todavía no hay movimiento en estos días.
  </div>

  <div v-else class="relative">
    <svg
      :viewBox="`0 0 ${ANCHO} ${ALTO}`"
      preserveAspectRatio="none"
      class="h-[170px] w-full"
      role="img"
      :aria-label="descripcion"
    >
      <line
        v-for="marca in marcas"
        :key="`grid-${marca}`"
        :x1="0"
        :x2="ANCHO"
        :y1="y(marca)"
        :y2="y(marca)"
        stroke="var(--color-hairline)"
        stroke-width="1"
        vector-effect="non-scaling-stroke"
      />

      <path :d="area('entradas')" :fill="series[0].color" opacity="0.1" />
      <path :d="area('salidas')" :fill="series[1].color" opacity="0.1" />

      <path
        :d="linea('entradas')"
        fill="none"
        :stroke="series[0].color"
        stroke-width="2"
        stroke-linejoin="round"
        stroke-linecap="round"
        vector-effect="non-scaling-stroke"
      />
      <path
        :d="linea('salidas')"
        fill="none"
        :stroke="series[1].color"
        stroke-width="2"
        stroke-linejoin="round"
        stroke-linecap="round"
        vector-effect="non-scaling-stroke"
      />

      <line
        v-if="indiceActivo !== null"
        :x1="x(indiceActivo)"
        :x2="x(indiceActivo)"
        :y1="MARGEN_SUP - 6"
        :y2="ALTO - MARGEN_INF"
        stroke="var(--color-ink-faint)"
        stroke-width="1"
        stroke-dasharray="3 3"
        vector-effect="non-scaling-stroke"
      />
    </svg>

    <!-- Los puntos van en HTML y no en el SVG: con preserveAspectRatio="none" el eje
         X se estira y un <circle> saldría ovalado. -->
    <template v-if="indiceActivo !== null && activo">
      <span
        v-for="(serie, i) in series"
        :key="serie.clave"
        aria-hidden="true"
        class="pointer-events-none absolute z-10 h-2 w-2 -translate-x-1/2 -translate-y-1/2 rounded-full"
        :style="{
          background: serie.color,
          left: `${proporcionDe(indiceActivo) * 100}%`,
          top: `${y(i === 0 ? activo.entradas : activo.salidas)}px`,
          boxShadow: '0 0 0 2px var(--color-vidrio-solido)',
        }"
      />
    </template>

    <!-- Zonas de contacto: una por día. Las de los extremos valen media franja para
         que el centro de cada zona caiga justo sobre su punto. -->
    <div class="absolute inset-0 flex">
      <button
        v-for="(punto, i) in datos"
        :key="punto.fecha"
        type="button"
        class="h-full basis-0 cursor-default outline-none"
        :style="{ flexGrow: i === 0 || i === datos.length - 1 ? 0.5 : 1 }"
        :aria-label="`${punto.etiqueta}: ${series[0].etiqueta} ${punto.entradas}, ${series[1].etiqueta} ${punto.salidas}`"
        @mouseenter="indiceActivo = i"
        @mouseleave="indiceActivo = null"
        @focus="indiceActivo = i"
        @blur="indiceActivo = null"
      />
    </div>

    <div
      v-if="activo"
      class="pointer-events-none absolute top-0 z-20 min-w-[9.5rem] rounded-xl border border-[var(--color-vidrio-borde)] bg-[var(--color-vidrio-solido)] p-3 shadow-lg backdrop-blur"
      :style="posicionTooltip"
    >
      <p class="text-xs font-semibold">{{ activo.etiqueta }}</p>
      <ul class="mt-2 space-y-1">
        <li v-for="(serie, i) in series" :key="serie.clave" class="flex items-center gap-2 text-[0.6875rem]">
          <span class="h-2 w-2 flex-none rounded-full" :style="{ background: serie.color }" />
          <span class="text-ink-soft flex-1">{{ serie.etiqueta }}</span>
          <span class="tabular-nums">{{ i === 0 ? activo.entradas : activo.salidas }}</span>
        </li>
      </ul>
    </div>

    <div class="text-ink-faint mt-1.5 flex justify-between text-[0.625rem] tabular-nums">
      <span>{{ datos[0]?.etiqueta }}</span>
      <span>{{ datos[datos.length - 1]?.etiqueta }}</span>
    </div>
  </div>
</template>
