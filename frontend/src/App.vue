<script setup lang="ts">
import ToastHost from '@/components/ToastHost.vue'
import PantallaCarga from '@/components/PantallaCarga.vue'
</script>

<template>
  <div class="relative min-h-screen">
    <RouterView v-slot="{ Component }">
      <Transition name="shell">
        <component :is="Component" />
      </Transition>
    </RouterView>
  </div>

  <ToastHost />
  <PantallaCarga />
</template>
