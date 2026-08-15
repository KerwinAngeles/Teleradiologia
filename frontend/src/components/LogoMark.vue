<script setup lang="ts">
withDefaults(defineProps<{ size?: number }>(), { size: 34 })
</script>

<template>
  <svg :width="size" :height="size" viewBox="0 0 34 34" fill="none" aria-hidden="true">
    <defs>
      <linearGradient id="markRing" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0%" stop-color="#ffb9a3" />
        <stop offset="55%" stop-color="#cbb9ff" />
        <stop offset="100%" stop-color="#a5e8dd" />
      </linearGradient>
    </defs>
    <circle cx="17" cy="17" r="16" fill="url(#markRing)" />
    <circle cx="17" cy="17" r="11" fill="#14131a" />
    <path d="M12 17h3l1.5-4 2.5 8 1.5-4h2" stroke="#ffffff" stroke-width="1.4" stroke-linecap="round" fill="none" />
  </svg>
</template>
