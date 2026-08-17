<script setup lang="ts">
import { ref, useSlots } from 'vue'
import type { Serie } from '@/composables/useEstadisticas'

defineProps<{
  titulo: string
  subtitulo: string
  // Con dos o más series la leyenda es obligatoria: el color nunca es la única pista.
  leyenda?: Serie[]
  nota?: string
}>()

const slots = useSlots()
const verTabla = ref(false)
</script>

<template>
  <section class="glass flex flex-col p-5">
    <div class="flex items-start justify-between gap-3">
      <div class="min-w-0">
        <h2 class="text-sm font-semibold">{{ titulo }}</h2>
        <p class="text-ink-faint mt-0.5 text-xs">{{ subtitulo }}</p>
      </div>
      <button
        v-if="slots.tabla"
        type="button"
        class="text-ink-faint hover:text-ink flex-none rounded-full border border-[var(--color-hairline)] px-2.5 py-1 text-[0.6875rem] tracking-[0.08em] uppercase transition-colors hover:bg-white/60"
        :aria-pressed="verTabla"
        @click="verTabla = !verTabla"
      >
        {{ verTabla ? 'Gráfico' : 'Datos' }}
      </button>
    </div>

    <ul v-if="leyenda?.length && !verTabla" class="mt-3.5 flex flex-wrap items-center gap-x-4 gap-y-1.5">
      <li v-for="serie in leyenda" :key="serie.clave" class="flex items-center gap-1.5">
        <span class="h-2 w-2 flex-none rounded-full" :style="{ background: serie.color }" />
        <span class="text-ink-soft text-[0.6875rem]">{{ serie.etiqueta }}</span>
      </li>
    </ul>

    <div class="mt-4 flex-1">
      <slot v-if="verTabla" name="tabla" />
      <slot v-else />
    </div>

    <p v-if="nota && !verTabla" class="text-ink-faint mt-4 text-[0.6875rem] leading-relaxed">{{ nota }}</p>
  </section>
</template>
