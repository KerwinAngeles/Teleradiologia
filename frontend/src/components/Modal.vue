<script setup lang="ts">
withDefaults(
  defineProps<{
    abierto: boolean
    titulo: string
    subtitulo?: string
    ancho?: 'md' | 'lg' | 'xl'
  }>(),
  { ancho: 'lg' },
)

const emit = defineEmits<{ cerrar: [] }>()

const anchos = {
  md: 'max-w-md',
  lg: 'max-w-2xl',
  xl: 'max-w-4xl',
} as const
</script>

<template>
  <Teleport to="body">
    <Transition name="modal">
      <div
        v-if="abierto"
        class="fixed inset-0 z-40 flex items-start justify-center overflow-y-auto p-4 py-10"
        role="dialog"
        aria-modal="true"
        @keydown.esc="emit('cerrar')"
      >
        <div class="fixed inset-0 bg-[#07070b]/60 backdrop-blur-sm" @click="emit('cerrar')" />

        <div class="glass-solid relative w-full shadow-2xl" :class="anchos[ancho]">
          <div class="flex items-start justify-between gap-4 border-b border-[var(--color-hairline)] px-7 py-5">
            <div>
              <h2 class="display text-xl">{{ titulo }}</h2>
              <p v-if="subtitulo" class="text-ink-soft mt-1.5 text-sm leading-relaxed">{{ subtitulo }}</p>
            </div>
            <button type="button" class="btn-orb flex-none" aria-label="Cerrar" @click="emit('cerrar')">
              <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div class="px-7 py-6">
            <slot />
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>
