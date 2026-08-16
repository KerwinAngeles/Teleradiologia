<script setup lang="ts">
import { ref, computed, onMounted, defineAsyncComponent } from 'vue'
import { useRoute } from 'vue-router'
import { api } from '@/services/api'
import InformePanel from '@/components/InformePanel.vue'
import type { Estudio, ImagenEstudio } from '@/types/estudio'
import type { Informe } from '@/types/informe'

const DicomViewer = defineAsyncComponent(() => import('@/components/DicomViewer.vue'))

const route = useRoute()
const estudioId = computed(() => route.params.id as string)

const estudio = ref<Estudio | null>(null)
const imagenes = ref<ImagenEstudio[]>([])
const informes = ref<Informe[]>([])
const cargando = ref(true)
const error = ref<string | null>(null)

async function cargarEstudio() {
  cargando.value = true
  error.value = null
  try {
    const [{ data: est }, { data: imgs }, { data: infs }] = await Promise.all([
      api.get<Estudio>(`/estudios/${estudioId.value}`),
      api.get<ImagenEstudio[]>(`/estudios/${estudioId.value}/imagenes`),
      api.get<Informe[]>(`/estudios/${estudioId.value}/informes`),
    ])
    estudio.value = est
    imagenes.value = imgs
    informes.value = infs
  } catch {
    error.value = 'No se pudo cargar el estudio.'
  } finally {
    cargando.value = false
  }
}

async function actualizarEstudioEInformes() {
  const [{ data: est }, { data: infs }] = await Promise.all([
    api.get<Estudio>(`/estudios/${estudioId.value}`),
    api.get<Informe[]>(`/estudios/${estudioId.value}/informes`),
  ])
  estudio.value = est
  informes.value = infs
}

onMounted(cargarEstudio)

const estadoChip: Record<string, string> = {
  Pendiente: 'chip-pendiente',
  EnInforme: 'chip-informe',
  Informado: 'chip-informado',
}
const estadoLabel: Record<string, string> = {
  Pendiente: 'Pendiente',
  EnInforme: 'En informe',
  Informado: 'Informado',
}

const inicialPaciente = computed(() => estudio.value?.pacienteNombre.charAt(0).toUpperCase() ?? '?')
const formatoFecha = new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
</script>

<template>
  <div class="stagger space-y-6">
    <RouterLink to="/" class="text-ink-soft hover:text-ink inline-flex items-center gap-2 text-sm transition-colors">
      <span class="btn-orb !h-8 !w-8">
        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.6" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
        </svg>
      </span>
      Volver al worklist
    </RouterLink>

    <div class="grid grid-cols-1 gap-5 lg:grid-cols-[1.35fr_1fr] xl:grid-cols-[1.5fr_1fr]">
      <DicomViewer
        v-if="!cargando"
        :estudio-id="estudioId"
        :imagenes="imagenes"
        :modalidad="estudio?.modalidad ?? '—'"
      />
      <section v-else class="glass p-3">
        <div class="flex min-h-[460px] items-center justify-center rounded-[1.1rem] bg-[#0b0b11]">
          <p class="text-sm text-white/40">Cargando estudio…</p>
        </div>
      </section>

      <div class="space-y-5">
        <section v-if="estudio" class="glass p-6">
          <div class="flex items-start justify-between gap-4">
            <div class="flex items-center gap-3.5">
              <span class="avatar-ring h-12 w-12 flex-none">
                <span class="text-base font-semibold">{{ inicialPaciente }}</span>
              </span>
              <div>
                <h1 class="text-lg leading-tight font-semibold">{{ estudio.pacienteNombre }}</h1>
                <p class="text-ink-faint text-xs">Doc. {{ estudio.pacienteDocumento }}</p>
              </div>
            </div>
            <span class="chip" :class="estadoChip[estudio.estado]">{{ estadoLabel[estudio.estado] }}</span>
          </div>

          <div class="mt-5">
            <div class="meta-row">
              <span class="meta-label">Modalidad</span>
              <span class="text-sm font-medium">{{ estudio.modalidad }}</span>
            </div>
            <div class="meta-row">
              <span class="meta-label">Hospital de origen</span>
              <span class="text-sm font-medium">{{ estudio.hospitalNombre }}</span>
            </div>
            <div class="meta-row">
              <span class="meta-label">Fecha del estudio</span>
              <span class="text-sm font-medium tabular-nums">
                {{ formatoFecha.format(new Date(estudio.fechaEstudio)) }}
              </span>
            </div>
            <div class="meta-row">
              <span class="meta-label">Cortes</span>
              <span class="text-sm font-medium tabular-nums">{{ imagenes.length }}</span>
            </div>
            <div class="meta-row">
              <span class="meta-label">Radiólogo</span>
              <span class="text-sm font-medium">{{ estudio.radiologoAsignadoNombre ?? 'Sin asignar' }}</span>
            </div>
          </div>

          <p v-if="estudio.descripcionEstudio" class="text-ink-soft mt-4 text-sm leading-relaxed">
            {{ estudio.descripcionEstudio }}
          </p>
        </section>

        <InformePanel v-if="estudio" :estudio="estudio" :informes="informes" @actualizar="actualizarEstudioEInformes" />
      </div>
    </div>

    <p v-if="error" class="text-sm text-red-700">{{ error }}</p>
  </div>
</template>
