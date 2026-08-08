<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { api } from '@/services/api'

interface HealthResponse {
  status: string
  database: string
  timestamp: string
}

const health = ref<HealthResponse | null>(null)
const error = ref<string | null>(null)

onMounted(async () => {
  try {
    const { data } = await api.get<HealthResponse>('/health')
    health.value = data
  } catch {
    error.value = 'No se pudo conectar con la API. ¿Está corriendo el backend?'
  }
})
</script>

<template>
  <main class="flex min-h-screen flex-col items-center justify-center gap-4 bg-slate-950 p-8 text-slate-100">
    <h1 class="text-3xl font-bold">Teleradiología — Esqueleto</h1>
    <p class="text-slate-400">Frontend Vue conectado al backend .NET.</p>

    <div class="mt-4 rounded-xl border border-slate-800 bg-slate-900 p-6 font-mono text-sm">
      <p v-if="health" class="text-emerald-400">
        API: {{ health.status }} · DB: {{ health.database }}
      </p>
      <p v-else-if="error" class="text-red-400">{{ error }}</p>
      <p v-else class="text-slate-500">Consultando /api/health…</p>
    </div>
  </main>
</template>
