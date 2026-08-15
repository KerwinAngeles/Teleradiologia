<script setup lang="ts">
import { computed, ref } from 'vue'

const props = defineProps<{
  titulo: string
  subtitulo: string
  datos: { etiqueta: string; valor: number }[]
}>()

const verTabla = ref(false)
const total = computed(() => props.datos.reduce((s, d) => s + d.valor, 0))
</script>

<template>
  <section class="glass flex flex-col p-5">
    <div class="flex items-start justify-between gap-3">
      <div>
        <h2 class="text-sm font-semibold">{{ titulo }}</h2>
        <p class="text-ink-faint mt-0.5 text-xs">{{ subtitulo }}</p>
      </div>
      <button
        type="button"
        class="text-ink-faint hover:text-ink flex-none rounded-full border border-[var(--color-hairline)] px-2.5 py-1 text-[0.6875rem] tracking-[0.08em] uppercase transition-colors hover:bg-white/60"
        @click="verTabla = !verTabla"
      >
        {{ verTabla ? 'Gráfico' : 'Datos' }}
      </button>
    </div>

    <div class="mt-5 flex-1">
      <table v-if="verTabla" class="w-full">
        <thead>
          <tr class="border-b border-[var(--color-hairline)]">
            <th class="meta-label py-1.5 text-left font-semibold">Categoría</th>
            <th class="meta-label py-1.5 text-right font-semibold">Estudios</th>
            <th class="meta-label py-1.5 text-right font-semibold">%</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="fila in datos" :key="fila.etiqueta" class="border-b border-[var(--color-hairline)] last:border-0">
            <td class="py-1.5 text-xs">{{ fila.etiqueta }}</td>
            <td class="py-1.5 text-right text-xs tabular-nums">{{ fila.valor }}</td>
            <td class="text-ink-faint py-1.5 text-right text-xs tabular-nums">
              {{ total === 0 ? '0%' : `${Math.round((fila.valor / total) * 100)}%` }}
            </td>
          </tr>
        </tbody>
      </table>

      <slot v-else />
    </div>
  </section>
</template>
