<script setup lang="ts">
import { ref, onMounted } from 'vue'
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

function onFileChange(evento: Event) {
  const input = evento.target as HTMLInputElement
  archivos.value = input.files ? Array.from(input.files) : []
}

async function onSubmit() {
  error.value = null

  if (archivos.value.length === 0) {
    error.value = 'Seleccioná al menos un archivo DICOM.'
    toasts.error(error.value)
    return
  }

  if (!hospitalId.value) {
    error.value = 'Elegí el hospital de origen.'
    toasts.error(error.value)
    return
  }

  const form = new FormData()
  for (const archivo of archivos.value) {
    form.append('Archivos', archivo)
  }
  form.append('HospitalId', hospitalId.value)
  form.append('Prioridad', prioridad.value)

  cargando.value = true
  const avisoEnCurso = toasts.cargando(
    `Subiendo ${archivos.value.length} archivo${archivos.value.length === 1 ? '' : 's'} a Orthanc…`,
  )
  try {
    const { data } = await api.post<Estudio>('/estudios', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    toasts.exito(`Estudio de ${data.pacienteNombre} subido — ${data.modalidad}, ya está en la worklist.`)
    archivos.value = []
    emit('subido', data)
  } catch (e) {
    const mensaje: string =
      isAxiosError(e) && e.response?.data?.message ? e.response.data.message : 'No se pudo subir el estudio.'
    error.value = mensaje
    toasts.error(mensaje)
  } finally {
    toasts.cerrar(avisoEnCurso)
    cargando.value = false
  }
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
        <p v-if="archivos.length > 0" class="meta-label mt-3">
          {{ archivos.length }} archivo{{ archivos.length === 1 ? '' : 's' }} seleccionado{{
            archivos.length === 1 ? '' : 's'
          }}
        </p>
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

      <button type="submit" :disabled="cargando" class="btn-ink w-full">
        {{ cargando ? 'Subiendo…' : 'Agregar resultado' }}
      </button>
  </form>
</template>
