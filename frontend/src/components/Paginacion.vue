<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  pagina: number
  tamanoPagina: number
  total: number
}>()

const emit = defineEmits<{ cambiar: [pagina: number] }>()

const totalPaginas = computed(() => (props.tamanoPagina > 0 ? Math.ceil(props.total / props.tamanoPagina) : 0))

const desde = computed(() => (props.total === 0 ? 0 : (props.pagina - 1) * props.tamanoPagina + 1))
const hasta = computed(() => Math.min(props.pagina * props.tamanoPagina, props.total))

// Ventana de páginas alrededor de la actual, con elipsis. Con 500 páginas no se pueden
// dibujar todos los botones.
const paginas = computed<(number | '…')[]>(() => {
  const ultimo = totalPaginas.value
  if (ultimo <= 7) return Array.from({ length: ultimo }, (_, i) => i + 1)

  const actual = props.pagina
  const resultado: (number | '…')[] = [1]

  const inicio = Math.max(2, actual - 1)
  const fin = Math.min(ultimo - 1, actual + 1)

  if (inicio > 2) resultado.push('…')
  for (let i = inicio; i <= fin; i++) resultado.push(i)
  if (fin < ultimo - 1) resultado.push('…')

  resultado.push(ultimo)
  return resultado
})
</script>

<template>
  <div v-if="total > 0" class="flex flex-wrap items-center justify-between gap-4 px-5 py-4">
    <p class="text-ink-faint text-xs tabular-nums">{{ desde }}–{{ hasta }} de {{ total }}</p>

    <div v-if="totalPaginas > 1" class="flex items-center gap-1">
      <button
        type="button"
        class="btn-orb !h-8 !w-8"
        :disabled="pagina <= 1"
        aria-label="Página anterior"
        @click="emit('cambiar', pagina - 1)"
      >
        <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" />
        </svg>
      </button>

      <template v-for="(p, i) in paginas" :key="`${p}-${i}`">
        <span v-if="p === '…'" class="text-ink-faint px-1.5 text-xs">…</span>
        <button
          v-else
          type="button"
          class="h-8 min-w-8 rounded-full px-2 text-xs font-medium tabular-nums transition-colors"
          :class="p === pagina ? 'nav-enlace-activo' : 'text-ink-soft hover:bg-[var(--color-superficie-suave)]'"
          @click="emit('cambiar', p)"
        >
          {{ p }}
        </button>
      </template>

      <button
        type="button"
        class="btn-orb !h-8 !w-8"
        :disabled="pagina >= totalPaginas"
        aria-label="Página siguiente"
        @click="emit('cambiar', pagina + 1)"
      >
        <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="m8.25 4.5 7.5 7.5-7.5 7.5" />
        </svg>
      </button>
    </div>
  </div>
</template>
