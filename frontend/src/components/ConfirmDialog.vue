<script setup lang="ts">
import { watch, nextTick, ref } from 'vue'

const props = withDefaults(
  defineProps<{
    abierto: boolean
    titulo: string
    mensaje: string
    textoConfirmar?: string
    textoCancelar?: string
    tono?: 'normal' | 'peligro'
  }>(),
  { textoConfirmar: 'Confirmar', textoCancelar: 'Cancelar', tono: 'normal' },
)

const emit = defineEmits<{ confirmar: []; cancelar: [] }>()

const botonConfirmar = ref<HTMLButtonElement | null>(null)

// Foco en confirmar: se resuelve con Enter o Escape.
watch(
  () => props.abierto,
  async (abierto) => {
    if (!abierto) return
    await nextTick()
    botonConfirmar.value?.focus()
  },
)
</script>

<template>
  <Teleport to="body">
    <Transition name="modal">
      <div
        v-if="abierto"
        class="fixed inset-0 z-40 flex items-center justify-center p-4"
        role="dialog"
        aria-modal="true"
        @keydown.esc="emit('cancelar')"
      >
        <div class="absolute inset-0 bg-[#07070b]/60 backdrop-blur-sm" @click="emit('cancelar')" />

        <div class="glass-solid relative w-full max-w-md p-6 shadow-2xl">
          <div class="flex items-start gap-4">
            <span
              class="flex h-10 w-10 flex-none items-center justify-center rounded-full"
              :class="tono === 'peligro' ? 'bg-amber-100 text-amber-700' : 'bg-[rgba(20,19,26,0.06)] text-ink-soft'"
            >
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v3.75m0 3.75h.007M12 3l9 16.5H3L12 3Z" />
              </svg>
            </span>
            <div class="flex-1">
              <h2 class="text-base font-semibold">{{ titulo }}</h2>
              <p class="text-ink-soft mt-1.5 text-sm leading-relaxed">{{ mensaje }}</p>
            </div>
          </div>

          <div v-if="$slots.default" class="mt-5">
            <slot />
          </div>

          <div class="mt-6 flex justify-end gap-3">
            <button type="button" class="btn-ghost" @click="emit('cancelar')">{{ textoCancelar }}</button>
            <button ref="botonConfirmar" type="button" class="btn-ink" @click="emit('confirmar')">
              {{ textoConfirmar }}
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>
