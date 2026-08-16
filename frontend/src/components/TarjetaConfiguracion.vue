<script setup lang="ts">
defineProps<{
  titulo: string
  descripcion: string
  destino?: string
}>()
</script>

<template>
  <component
    :is="destino ? 'RouterLink' : 'div'"
    :to="destino"
    class="glass group block p-6 transition-transform"
    :class="destino ? 'hover:-translate-y-0.5' : ''"
  >
    <span
      class="flex h-11 w-11 items-center justify-center rounded-2xl bg-[var(--color-superficie-suave)] transition-colors"
      :class="destino ? 'group-hover:bg-ink group-hover:text-[var(--color-sobre-tinta)]' : ''"
    >
      <slot name="icono" />
    </span>

    <h3 class="mt-5 text-base font-semibold">{{ titulo }}</h3>
    <p class="text-ink-soft mt-1.5 line-clamp-2 text-sm leading-relaxed">{{ descripcion }}</p>

    <slot />
  </component>
</template>
