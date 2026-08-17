<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { api } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { Plantilla } from '@/types/plantilla'

const props = defineProps<{ modalidad?: string | null }>()
const emit = defineEmits<{ aplicar: [texto: string] }>()

const toasts = useToastStore()

const plantillas = ref<Plantilla[]>([])
const cargando = ref(true)
const abierto = ref(false)
const busqueda = ref('')
const aplicando = ref<string | null>(null)

onMounted(async () => {
  try {
    const { data } = await api.get<Plantilla[]>('/plantillas', {
      params: { modalidad: props.modalidad || undefined },
    })
    plantillas.value = data
  } catch {
    plantillas.value = []
  } finally {
    cargando.value = false
  }
})

const visibles = computed(() => {
  const texto = busqueda.value.trim().toLowerCase()
  if (!texto) return plantillas.value
  return plantillas.value.filter(
    (p) => p.nombre.toLowerCase().includes(texto) || (p.regionAnatomica ?? '').toLowerCase().includes(texto),
  )
})

async function aplicar(p: Plantilla) {
  aplicando.value = p.id
  try {
    const { data } = await api.post<string>(`/plantillas/${p.id}/aplicar`)
    emit('aplicar', data)
    abierto.value = false
    toasts.info(`Plantilla «${p.nombre}» aplicada.`)
  } catch {
    toasts.error('No se pudo aplicar la plantilla.')
  } finally {
    aplicando.value = null
  }
}
</script>

<template>
  <div v-if="!cargando && plantillas.length > 0" class="mb-3">
    <button
      type="button"
      class="btn-ghost !py-2 !text-xs"
      @click="abierto = !abierto"
    >
      <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25H12"
        />
      </svg>
      Usar plantilla
      <span class="chip chip-neutro !px-2 !py-0">{{ plantillas.length }}</span>
    </button>

    <Transition name="fade-slide">
      <div v-if="abierto" class="mt-3 rounded-2xl border border-[var(--color-borde)] bg-[var(--color-campo)] p-3">
        <input
          v-if="plantillas.length > 4"
          v-model="busqueda"
          type="search"
          placeholder="Buscar plantilla…"
          class="field mb-3 !py-1.5 text-sm"
        />

        <div class="grid max-h-72 grid-cols-1 gap-2 overflow-y-auto sm:grid-cols-2">
          <button
            v-for="p in visibles"
            :key="p.id"
            type="button"
            :disabled="aplicando !== null"
            class="relative overflow-hidden rounded-xl border border-[var(--color-borde)] p-3 text-left transition-colors hover:bg-[var(--color-superficie-suave)] disabled:opacity-50"
            @click="aplicar(p)"
          >
            <span
              v-if="p.favorita"
              class="absolute inset-y-0 left-0 w-1 bg-gradient-to-b from-[var(--color-coral)] via-[var(--color-lilac)] to-[var(--color-aqua)]"
            />
            <span class="flex items-start justify-between gap-2">
              <span class="min-w-0 flex-1">
                <span class="block truncate text-sm font-medium">{{ p.nombre }}</span>
                <span class="text-ink-faint block truncate text-xs">
                  {{ p.secciones.length }} secciones<template v-if="p.regionAnatomica"> · {{ p.regionAnatomica }}</template>
                </span>
              </span>
              <span v-if="p.modalidad" class="chip chip-informe flex-none !px-2">{{ p.modalidad }}</span>
            </span>
          </button>

          <p v-if="visibles.length === 0" class="text-ink-faint col-span-full py-6 text-center text-sm">
            Sin coincidencias.
          </p>
        </div>

        <p class="text-ink-faint mt-3 text-xs">
          El texto de la plantilla reemplaza lo que haya escrito en el borrador.
        </p>
      </div>
    </Transition>
  </div>
</template>
