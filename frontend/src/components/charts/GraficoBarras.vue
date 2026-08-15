<script setup lang="ts">
import { computed } from 'vue'
import type { Barra } from '@/composables/useEstadisticas'

const props = defineProps<{ datos: Barra[] }>()

const maximo = computed(() => Math.max(1, ...props.datos.map((d) => d.valor)))
const total = computed(() => props.datos.reduce((s, d) => s + d.valor, 0))

function ancho(valor: number) {
  return `${(valor / maximo.value) * 100}%`
}

function porcentaje(valor: number) {
  return total.value === 0 ? '0%' : `${Math.round((valor / total.value) * 100)}%`
}
</script>

<template>
  <div v-if="datos.length === 0" class="text-ink-faint flex h-full min-h-[180px] items-center justify-center text-sm">
    Todavía no hay datos.
  </div>

  <ul v-else class="space-y-1">
    <li
      v-for="barra in datos"
      :key="barra.etiqueta"
      class="group grid grid-cols-[minmax(4.5rem,7rem)_1fr_auto] items-center gap-3 rounded-lg py-1.5 transition-colors hover:bg-white/50"
      :title="`${barra.etiqueta}: ${barra.valor} (${porcentaje(barra.valor)} del total)`"
    >
      <span class="truncate text-xs font-medium" :title="barra.etiqueta">{{ barra.etiqueta }}</span>

      <span class="relative flex h-3.5 items-center border-l border-[var(--color-hairline)]">
        <span
          class="h-full min-w-[2px] rounded-r-[4px] transition-[width] duration-500 ease-out"
          :style="{ width: ancho(barra.valor), background: 'var(--color-viz-serie)' }"
        />
      </span>

      <span class="text-ink-soft w-8 text-right text-xs tabular-nums">{{ barra.valor }}</span>
    </li>
  </ul>
</template>
