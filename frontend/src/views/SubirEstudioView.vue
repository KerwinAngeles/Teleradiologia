<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { isAxiosError } from 'axios'
import { api } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import type { Estudio } from '@/types/estudio'

const router = useRouter()
const toasts = useToastStore()

const archivos = ref<File[]>([])
const hospitalOrigen = ref('')
const cargando = ref(false)
const error = ref<string | null>(null)

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

  const form = new FormData()
  for (const archivo of archivos.value) {
    form.append('Archivos', archivo)
  }
  form.append('HospitalOrigen', hospitalOrigen.value)

  cargando.value = true
  const avisoEnCurso = toasts.cargando(
    `Subiendo ${archivos.value.length} archivo${archivos.value.length === 1 ? '' : 's'} a Orthanc…`,
  )
  try {
    const { data } = await api.post<Estudio>('/estudios', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    toasts.exito(`Estudio de ${data.pacienteNombre} subido — ${data.modalidad}, ya está en la worklist.`)
    router.push(`/estudios/${data.id}`)
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
  <div class="stagger mx-auto max-w-3xl space-y-6">
    <div>
      <p class="meta-label">Ingreso de estudios</p>
      <h1 class="display mt-1.5 text-3xl sm:text-4xl">Subir estudio</h1>
      <p class="text-ink-soft mt-3 max-w-xl text-sm leading-relaxed">
        Seleccioná los archivos DICOM del estudio (una o varias instancias). La metadata del paciente y del estudio se
        extrae de los propios tags — no hay que tipearla.
      </p>
    </div>

    <form class="glass space-y-6 p-7" @submit.prevent="onSubmit">
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
        <input id="hospital" v-model="hospitalOrigen" type="text" required placeholder="Clínica del Valle" class="field" />
      </div>

      <p v-if="error" class="rounded-xl bg-red-500/10 px-3 py-2 text-sm text-red-700">{{ error }}</p>

      <button type="submit" :disabled="cargando" class="btn-ink w-full">
        {{ cargando ? 'Subiendo…' : 'Subir estudio' }}
      </button>
    </form>
  </div>
</template>
