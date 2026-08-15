<script setup lang="ts">
import { useToastStore } from '@/stores/toast'

const toasts = useToastStore()

const estilo: Record<string, string> = {
  exito: 'border-emerald-300/60 bg-emerald-50/90 text-emerald-900',
  error: 'border-red-300/60 bg-red-50/90 text-red-900',
  info: 'border-white/70 bg-white/85',
  cargando: 'border-white/70 bg-white/85',
}
</script>

<template>
  <Teleport to="body">
    <div class="pointer-events-none fixed right-4 bottom-4 z-50 flex w-full max-w-sm flex-col gap-2">
      <TransitionGroup name="toast">
        <div
          v-for="toast in toasts.toasts"
          :key="toast.id"
          class="pointer-events-auto flex items-start gap-3 rounded-2xl border px-4 py-3 shadow-lg backdrop-blur-xl"
          :class="estilo[toast.tipo]"
          role="status"
        >
          <span class="mt-0.5 flex-none">
            <svg
              v-if="toast.tipo === 'exito'"
              class="h-4 w-4"
              fill="none"
              viewBox="0 0 24 24"
              stroke-width="2"
              stroke="currentColor"
            >
              <path stroke-linecap="round" stroke-linejoin="round" d="m4.5 12.75 6 6 9-13.5" />
            </svg>
            <svg
              v-else-if="toast.tipo === 'error'"
              class="h-4 w-4"
              fill="none"
              viewBox="0 0 24 24"
              stroke-width="2"
              stroke="currentColor"
            >
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v3.75m0 3.75h.007M12 3l9 16.5H3L12 3Z" />
            </svg>
            <span
              v-else-if="toast.tipo === 'cargando'"
              class="block h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent"
            />
            <svg v-else class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" d="M11.25 11.25h1.5v5.25m-1.5-9h.75v.75h-.75z" />
            </svg>
          </span>

          <p class="flex-1 text-sm leading-snug">{{ toast.mensaje }}</p>

          <button
            v-if="toast.tipo !== 'cargando'"
            type="button"
            class="-mr-1 flex-none rounded-full p-1 opacity-50 transition-opacity hover:opacity-100"
            aria-label="Cerrar aviso"
            @click="toasts.cerrar(toast.id)"
          >
            <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2.2" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>
