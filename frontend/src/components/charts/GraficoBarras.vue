<script setup lang="ts">
import { computed } from 'vue'
import type { BarraApilada } from '@/composables/useEstadisticas'

const props = defineProps<{ datos: BarraApilada[] }>()

const maximo = computed(() => Math.max(1, ...props.datos.map((d) => d.total)))
const totalGeneral = computed(() => props.datos.reduce((s, d) => s + d.total, 0))

// Marcas de referencia en 0 / 25 / 50 / 75 / 100 % del máximo: sin ellas la
// comparación entre barras se vuelve a ojo.
const marcas = computed(() => {
  const paso = maximo.value <= 4 ? 1 : maximo.value / 4
  const marcasCalculadas: number[] = []
  for (let v = 0; v <= maximo.value + 0.001; v += paso) marcasCalculadas.push(Math.round(v))
  return [...new Set(marcasCalculadas)]
})

function porcentajeDelMaximo(valor: number) {
  return `${(valor / maximo.value) * 100}%`
}

function participacion(valor: number) {
  return totalGeneral.value === 0 ? '0%' : `${Math.round((valor / totalGeneral.value) * 100)}%`
}

function descripcion(barra: BarraApilada) {
  const partes = barra.segmentos
    .filter((s) => s.valor > 0)
    .map((s) => `${s.etiqueta}: ${s.valor}`)
    .join(', ')
  return `${barra.etiqueta}, ${barra.total} estudios${partes ? ` (${partes})` : ''}`
}
</script>

<template>
  <div v-if="datos.length === 0" class="text-ink-faint flex h-full min-h-[180px] items-center justify-center text-sm">
    Todavía no hay datos.
  </div>

  <div v-else role="img" :aria-label="datos.map(descripcion).join('. ')">
    <ul class="space-y-0.5">
      <li
        v-for="barra in datos"
        :key="barra.etiqueta"
        tabindex="0"
        class="group relative grid grid-cols-[minmax(4.5rem,6.5rem)_1fr_auto] items-center gap-3 rounded-lg py-1.5 outline-none transition-colors hover:bg-white/50 focus-visible:bg-white/50"
      >
        <span class="truncate text-xs font-medium" :title="barra.etiqueta">{{ barra.etiqueta }}</span>

        <!-- Pista con la línea base a la izquierda: el cero siempre está anclado. -->
        <span class="relative flex h-4 items-center border-l border-[var(--color-hairline)]">
          <span
            v-for="marca in marcas.slice(1)"
            :key="`marca-${marca}`"
            aria-hidden="true"
            class="absolute top-0 bottom-0 w-px bg-[var(--color-hairline)] opacity-60"
            :style="{ left: porcentajeDelMaximo(marca) }"
          />

          <span class="relative flex h-full w-full gap-[2px]">
            <span
              v-for="segmento in barra.segmentos.filter((s) => s.valor > 0)"
              :key="segmento.clave"
              class="h-full min-w-[3px] transition-[width] duration-500 ease-out last:rounded-r-[4px]"
              :style="{ width: porcentajeDelMaximo(segmento.valor), background: segmento.color }"
            />
          </span>
        </span>

        <span class="text-ink-soft w-8 text-right text-xs tabular-nums">{{ barra.total }}</span>

        <div
          class="pointer-events-none absolute top-full left-[4.5rem] z-20 hidden min-w-[11rem] rounded-xl border border-[var(--color-vidrio-borde)] bg-[var(--color-vidrio-solido)] p-3 shadow-lg backdrop-blur group-hover:block group-focus-visible:block"
        >
          <p class="text-xs font-semibold">{{ barra.etiqueta }}</p>
          <p class="text-ink-faint mt-0.5 text-[0.6875rem]">
            {{ barra.total }} estudios · {{ participacion(barra.total) }} del total
          </p>
          <ul class="mt-2 space-y-1">
            <li
              v-for="segmento in barra.segmentos"
              :key="segmento.clave"
              class="flex items-center gap-2 text-[0.6875rem]"
            >
              <span class="h-2 w-2 flex-none rounded-full" :style="{ background: segmento.color }" />
              <span class="text-ink-soft flex-1">{{ segmento.etiqueta }}</span>
              <span class="tabular-nums">{{ segmento.valor }}</span>
            </li>
          </ul>
        </div>
      </li>
    </ul>

    <div class="mt-2 grid grid-cols-[minmax(4.5rem,6.5rem)_1fr_auto] gap-3">
      <span />
      <span class="relative block h-4">
        <span
          v-for="marca in marcas"
          :key="`eje-${marca}`"
          class="text-ink-faint absolute text-[0.625rem] tabular-nums"
          :class="marca === 0 ? 'left-0' : '-translate-x-1/2'"
          :style="marca === 0 ? undefined : { left: porcentajeDelMaximo(marca) }"
        >
          {{ marca }}
        </span>
      </span>
      <span class="w-8" />
    </div>
  </div>
</template>
