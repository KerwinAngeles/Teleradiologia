<script setup lang="ts">
withDefaults(defineProps<{ size?: number }>(), { size: 340 })
</script>

<template>
  <svg :width="size" :height="size" viewBox="0 0 340 340" fill="none" aria-hidden="true" class="max-w-full">
    <defs>
      <radialGradient id="orbCore" cx="38%" cy="32%" r="72%">
        <stop offset="0%" stop-color="#fff1ea" />
        <stop offset="35%" stop-color="#ffb9a3" />
        <stop offset="70%" stop-color="#cbb9ff" />
        <stop offset="100%" stop-color="#a5e8dd" />
      </radialGradient>
      <linearGradient id="orbSweep" x1="0" y1="0" x2="1" y2="0">
        <stop offset="0%" stop-color="#ffffff" stop-opacity="0" />
        <stop offset="50%" stop-color="#ffffff" stop-opacity="0.85" />
        <stop offset="100%" stop-color="#ffffff" stop-opacity="0" />
      </linearGradient>
      <filter id="orbBlur" x="-30%" y="-30%" width="160%" height="160%">
        <feGaussianBlur stdDeviation="18" />
      </filter>
      <clipPath id="orbClip">
        <circle cx="170" cy="170" r="118" />
      </clipPath>
    </defs>

    <circle cx="170" cy="170" r="124" fill="url(#orbCore)" filter="url(#orbBlur)" opacity="0.75" class="pulse-slow" />

    <circle cx="170" cy="170" r="112" fill="url(#orbCore)" opacity="0.95" />

    <g clip-path="url(#orbClip)">
      <g stroke="#ffffff" stroke-opacity="0.55" stroke-width="1">
        <line v-for="i in 17" :key="`h${i}`" :x1="52" :x2="288" :y1="52 + (i - 1) * 14" :y2="52 + (i - 1) * 14" />
      </g>
      <path
        d="M170 78c34 0 60 26 60 60 0 18-7 27-7 40 0 10 6 13 6 22 0 12-11 20-26 20h-12c-4 12-12 18-24 18-24 0-46-20-52-46-3-13-5-24-5-34 0-46 26-80 60-80z"
        fill="#ffffff"
        fill-opacity="0.28"
        stroke="#14131a"
        stroke-opacity="0.28"
        stroke-width="1.25"
      />
      <path
        d="M150 118c14 6 20 20 14 32-5 10-2 18 6 22M186 132c-10 10-10 22 0 30 8 6 8 16 0 24"
        stroke="#14131a"
        stroke-opacity="0.22"
        stroke-width="1.25"
        fill="none"
      />
      <rect x="52" y="150" width="236" height="40" fill="url(#orbSweep)" class="drift" />
    </g>

    <circle cx="170" cy="170" r="112" fill="none" stroke="#14131a" stroke-opacity="0.14" />
    <circle cx="170" cy="170" r="130" fill="none" stroke="#14131a" stroke-opacity="0.1" stroke-dasharray="2 8" />
    <circle
      cx="170"
      cy="170"
      r="150"
      fill="none"
      stroke="#14131a"
      stroke-opacity="0.08"
      stroke-dasharray="1 14"
      class="pulse-slow"
    />

    <g stroke="#14131a" stroke-opacity="0.3" stroke-width="1.5">
      <line x1="170" y1="14" x2="170" y2="26" />
      <line x1="170" y1="314" x2="170" y2="326" />
      <line x1="14" y1="170" x2="26" y2="170" />
      <line x1="314" y1="170" x2="326" y2="170" />
    </g>
  </svg>
</template>
