<script setup lang="ts">
import { useNavegacionStore } from '@/stores/navegacion'
import LogoMark from '@/components/LogoMark.vue'

const navegacion = useNavegacionStore()
</script>

<template>
  <Teleport to="body">
    <Transition name="carga">
      <div
        v-if="navegacion.cargando"
        class="fixed inset-0 z-50 flex flex-col items-center justify-center gap-5"
        role="status"
        aria-live="polite"
      >
        <div class="absolute inset-0 bg-[var(--color-lienzo)]/85 backdrop-blur-md" />

        <div class="relative flex h-24 w-24 items-center justify-center">
          <svg class="absolute inset-0 h-24 w-24 animate-spin" viewBox="0 0 96 96" fill="none" aria-hidden="true">
            <defs>
              <linearGradient id="cargaAnillo" x1="0" y1="0" x2="1" y2="1">
                <stop offset="0%" stop-color="#ffb9a3" />
                <stop offset="55%" stop-color="#cbb9ff" />
                <stop offset="100%" stop-color="#a5e8dd" />
              </linearGradient>
            </defs>
            <circle cx="48" cy="48" r="44" stroke="var(--color-hairline)" stroke-width="3" />
            <circle
              cx="48"
              cy="48"
              r="44"
              stroke="url(#cargaAnillo)"
              stroke-width="3"
              stroke-linecap="round"
              stroke-dasharray="70 210"
            />
          </svg>
          <LogoMark :size="42" />
        </div>

        <p class="meta-label relative">Cargando</p>
      </div>
    </Transition>
  </Teleport>
</template>
