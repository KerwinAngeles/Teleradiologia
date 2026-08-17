<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { isAxiosError } from 'axios'
import { api } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { Estudio } from '@/types/estudio'
import type { Hospital } from '@/types/hospital'
import type { PrioridadEstudio } from '@/types/estudio'

const emit = defineEmits<{ subido: [estudio: Estudio] }>()

const toasts = useToastStore()

const archivos = ref<File[]>([])
const hospitales = ref<Hospital[]>([])
const hospitalId = ref('')
const prioridad = ref<PrioridadEstudio>('Rutina')
const cargando = ref(false)
const error = ref<string | null>(null)

onMounted(async () => {
  try {
    const { data } = await api.get<Hospital[]>('/hospitales')
    hospitales.value = data
    if (data.length === 1) hospitalId.value = data[0].id
  } catch {
    error.value = 'No se pudieron cargar los hospitales.'
  }
})

const prioridades = [
  { valor: 'Rutina' as const, etiqueta: 'Rutina', chip: 'chip-neutro', detalle: 'Lectura programada. 24 h de plazo.' },
  { valor: 'Urgente' as const, etiqueta: 'Urgente', chip: 'chip-urgente', detalle: 'Requiere lectura pronta. 2 h de plazo.' },
  { valor: 'Stat' as const, etiqueta: 'STAT', chip: 'chip-stat', detalle: 'Emergencia. 30 min de plazo.' },
]

// El API acepta hasta 200 MB por petición ([RequestSizeLimit] en EstudiosController).
// Se deja margen para el overhead del multipart.
const LIMITE_PETICION = 195 * 1024 * 1024

const subidos = ref(0)
const bytesEnviados = ref(0)
const archivoActual = ref('')
const fallidos = ref<string[]>([])

function onFileChange(evento: Event) {
  const input = evento.target as HTMLInputElement
  archivos.value = input.files ? Array.from(input.files) : []
  error.value = null
}

function enMb(bytes: number): string {
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`
}

const pesoTotal = computed(() => archivos.value.reduce((suma, a) => suma + a.size, 0))

const demasiadoGrandes = computed(() => archivos.value.filter((a) => a.size > LIMITE_PETICION))

// Cada archivo va en su propia petición, así que el límite es por archivo y no por lote.
// Se avisa antes de subir: sin esto el navegador manda todo y recién ahí el servidor rechaza.
const problema = computed<string | null>(() => {
  if (demasiadoGrandes.value.length === 0) return null

  const nombres = demasiadoGrandes.value.map((a) => `${a.name} (${enMb(a.size)})`).join(', ')
  return `Estos archivos superan el máximo de ${enMb(LIMITE_PETICION)} por archivo: ${nombres}.`
})

async function onSubmit() {
  error.value = null

  if (archivos.value.length === 0) {
    error.value = 'Seleccioná al menos un archivo DICOM.'
    toasts.error(error.value)
    return
  }

  if (problema.value) {
    error.value = problema.value
    toasts.error(problema.value)
    return
  }

  if (!hospitalId.value) {
    error.value = 'Elegí el hospital de origen.'
    toasts.error(error.value)
    return
  }

  const lista = archivos.value
  const total = lista.length

  cargando.value = true
  subidos.value = 0
  bytesEnviados.value = 0
  fallidos.value = []

  let ultimo: Estudio | null = null
  let bytesCompletados = 0

  for (const [indice, archivo] of lista.entries()) {
    archivoActual.value = `${indice + 1} de ${total} · ${archivo.name}`

    const form = new FormData()
    form.append('Archivos', archivo)
    form.append('HospitalId', hospitalId.value)
    form.append('Prioridad', prioridad.value)

    try {
      const { data } = await api.post<Estudio>('/estudios', form, {
        headers: { 'Content-Type': 'multipart/form-data' },
        onUploadProgress: (evento) => {
          bytesEnviados.value = bytesCompletados + evento.loaded
        },
      })
      ultimo = data
      subidos.value++
    } catch (e) {
      fallidos.value.push(`${archivo.name}: ${mensajeDeError(e)}`)
    }

    bytesCompletados += archivo.size
    bytesEnviados.value = bytesCompletados
  }

  archivoActual.value = ''
  cargando.value = false

  if (ultimo && fallidos.value.length === 0) {
    toasts.exito(
      `Estudio de ${ultimo.pacienteNombre} subido — ${ultimo.modalidad}, ` +
        `${total} imagen${total === 1 ? '' : 'es'}. Ya está en la worklist.`,
    )
    archivos.value = []
    emit('subido', ultimo)
    return
  }

  // Parcial: lo que entró queda cargado, así que se avisa qué faltó sin perder el resto.
  if (ultimo) {
    error.value = `Se subieron ${subidos.value} de ${total}. Falló: ${fallidos.value.join(' · ')}`
    toasts.error(`${fallidos.value.length} archivo(s) no se pudieron subir.`)
    emit('subido', ultimo)
    return
  }

  error.value = fallidos.value.join(' · ') || 'No se pudo subir el estudio.'
  toasts.error('No se pudo subir el estudio.')
}

function mensajeDeError(e: unknown): string {
  if (!isAxiosError(e)) return 'error inesperado'
  if (e.response?.status === 413) return 'el archivo excede el límite del servidor'
  return e.response?.data?.message ?? e.response?.data?.detail ?? `error ${e.response?.status ?? 'de red'}`
}
</script>

<template>
  <form class="space-y-6" @submit.prevent="onSubmit">
      <div>
        <label class="meta-label mb-2 block" for="archivos">Archivos DICOM</label>
        <label
          for="archivos"
          class="hover:border-ink/40 flex cursor-pointer flex-col items-center justify-center gap-2 rounded-[1.1rem] border border-dashed border-[rgba(20,19,26,0.22)] bg-white/50 px-6 py-10 text-center transition-colors hover:bg-white/70"
        >
          <span class="avatar-ring h-11 w-11">
            <span>
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  d="M12 16.5V9m0 0L8.25 12.75M12 9l3.75 3.75M3 16.5v.75A3.75 3.75 0 0 0 6.75 21h10.5A3.75 3.75 0 0 0 21 17.25v-.75"
                />
              </svg>
            </span>
          </span>
          <span class="text-sm font-medium">Elegí los archivos .dcm</span>
          <span class="text-ink-faint text-xs">Podés seleccionar todas las instancias de la serie a la vez</span>
          <input
            id="archivos"
            type="file"
            multiple
            accept=".dcm,application/dicom"
            class="sr-only"
            @change="onFileChange"
          />
        </label>
        <div v-if="archivos.length > 0" class="mt-3 space-y-2">
          <div class="flex flex-wrap items-center justify-between gap-2">
            <p class="meta-label">
              <template v-if="cargando">Subiendo {{ archivoActual }}</template>
              <template v-else>
                {{ archivos.length }} archivo{{ archivos.length === 1 ? '' : 's' }} seleccionado{{
                  archivos.length === 1 ? '' : 's'
                }}
              </template>
            </p>
            <p class="text-xs tabular-nums" :class="problema ? 'text-red-700' : 'text-ink-faint'">
              <template v-if="cargando">{{ enMb(bytesEnviados) }} de {{ enMb(pesoTotal) }}</template>
              <template v-else>{{ enMb(pesoTotal) }}</template>
            </p>
          </div>

          <div class="h-1.5 overflow-hidden rounded-full bg-[var(--color-superficie-suave)]">
            <div
              class="h-full rounded-full transition-all"
              :class="problema ? 'bg-red-500' : 'bg-[var(--color-estado-informado)]'"
              :style="{
                width: cargando
                  ? `${Math.min(100, (bytesEnviados / Math.max(1, pesoTotal)) * 100)}%`
                  : problema
                    ? '100%'
                    : '0%',
              }"
            />
          </div>

          <p v-if="problema" class="rounded-xl bg-red-500/10 px-3 py-2 text-xs leading-relaxed text-red-700">
            {{ problema }}
          </p>
          <p v-else-if="!cargando && archivos.length > 1" class="text-ink-faint text-xs">
            Se suben de a uno y se agrupan solos en un mismo estudio.
          </p>
        </div>
      </div>

      <div>
        <label class="meta-label mb-2 block" for="hospital">Hospital de origen</label>
        <select id="hospital" v-model="hospitalId" required class="field">
          <option value="" disabled>Elegí un hospital…</option>
          <option v-for="h in hospitales" :key="h.id" :value="h.id">
            {{ h.nombre }}{{ h.provincia ? ` — ${h.provincia}` : '' }}
          </option>
        </select>
        <p v-if="hospitales.length === 0" class="text-ink-faint mt-1.5 text-xs">
          No tenés hospitales habilitados. Pedile a un administrador que te asigne uno.
        </p>
      </div>

      <div>
        <label class="meta-label mb-2 block">Prioridad</label>
        <div class="grid grid-cols-1 gap-3 sm:grid-cols-3">
          <button
            v-for="p in prioridades"
            :key="p.valor"
            type="button"
            class="rounded-2xl border p-4 text-left transition-colors"
            :class="
              prioridad === p.valor
                ? 'border-[var(--color-borde-fuerte)] bg-[var(--color-superficie-suave)]'
                : 'border-[var(--color-borde)] hover:bg-[var(--color-superficie-suave)]'
            "
            @click="prioridad = p.valor"
          >
            <span class="chip" :class="p.chip">{{ p.etiqueta }}</span>
            <span class="text-ink-soft mt-2 block text-xs leading-relaxed">{{ p.detalle }}</span>
          </button>
        </div>
        <p class="text-ink-faint mt-2 text-xs">
          Define el plazo de entrega. El reloj arranca cuando el estudio entra, no cuando lo toma el radiólogo.
        </p>
      </div>

      <p v-if="error" class="rounded-xl bg-red-500/10 px-3 py-2 text-sm text-red-700">{{ error }}</p>

      <button type="submit" :disabled="cargando || problema !== null" class="btn-ink w-full">
        {{ cargando ? `Subiendo ${subidos + 1} de ${archivos.length}…` : 'Agregar resultado' }}
      </button>
  </form>
</template>
