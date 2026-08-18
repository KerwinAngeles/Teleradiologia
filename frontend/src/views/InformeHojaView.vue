<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import { comoHtmlSeguro } from '@/services/informeHtml'
import HojaInforme from '@/components/HojaInforme.vue'
import type { InformeDetalle, VerificacionFirma } from '@/types/informe'

const route = useRoute()
const router = useRouter()
const toasts = useToastStore()

const informeId = route.params.id as string

const informe = ref<InformeDetalle | null>(null)
const cargando = ref(true)
const error = ref<string | null>(null)
const verificando = ref(false)
const verificacion = ref<VerificacionFirma | null>(null)

const firmado = computed(() => informe.value?.estado === 'Firmado')
const cuerpo = computed(() => (informe.value ? comoHtmlSeguro(informe.value.contenido) : ''))

async function cargar() {
  try {
    const { data } = await api.get<InformeDetalle>(`/informes/${informeId}`)
    informe.value = data
  } catch {
    error.value = 'No se pudo cargar el informe.'
  } finally {
    cargando.value = false
  }
}

async function verificarFirma() {
  verificando.value = true
  try {
    const { data } = await api.get<VerificacionFirma>(`/informes/${informeId}/verificacion`)
    verificacion.value = data
    if (data.valida) toasts.exito('Firma válida: el contenido es el que se firmó.')
    else toasts.error(data.motivo ?? 'La firma no pudo validarse.')
  } catch {
    toasts.error('No se pudo verificar la firma.')
  } finally {
    verificando.value = false
  }
}

function exportarPdf() {
  window.print()
}

onMounted(cargar)
</script>

<template>
  <div class="min-h-screen bg-[var(--color-lienzo)]">
    <!-- Barra de trabajo: no se imprime. -->
    <header
      class="sin-imprimir sticky top-0 z-20 border-b border-[var(--color-hairline)] bg-[var(--color-vidrio-solido)] backdrop-blur"
    >
      <div class="mx-auto flex max-w-[1100px] flex-wrap items-center justify-between gap-3 px-5 py-3">
        <div class="flex items-center gap-3">
          <button type="button" class="btn-orb" title="Volver a los informes" @click="router.push('/informes')">
            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
            </svg>
          </button>
          <div>
            <p class="meta-label">{{ informe?.esAdenda ? 'Adenda' : 'Informe radiológico' }}</p>
            <p class="text-sm font-medium">{{ informe?.pacienteNombre ?? '…' }}</p>
          </div>
          <span v-if="informe" class="chip" :class="firmado ? 'chip-informado' : 'chip-neutro'">
            {{ firmado ? 'Firmado' : 'Borrador' }}
          </span>
        </div>

        <div class="flex items-center gap-2">
          <button
            v-if="firmado"
            type="button"
            :disabled="verificando"
            class="btn-ghost !py-2 !text-xs"
            @click="verificarFirma"
          >
            {{ verificando ? 'Verificando…' : 'Verificar firma' }}
          </button>
          <button type="button" :disabled="!informe" class="btn-ink !py-2 !text-xs" @click="exportarPdf">
            <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" d="M6.72 13.829c-.24.03-.48.062-.72.096m.72-.096a42.415 42.415 0 0 1 10.56 0m-10.56 0L6.34 18m10.94-4.171c.24.03.48.062.72.096m-.72-.096L17.66 18m0 0 .229 2.523a1.125 1.125 0 0 1-1.12 1.227H7.231c-.662 0-1.18-.568-1.12-1.227L6.34 18m11.318 0h1.091A2.25 2.25 0 0 0 21 15.75V9.456c0-1.081-.768-2.015-1.837-2.175a48.055 48.055 0 0 0-1.913-.247M6.34 18H5.25A2.25 2.25 0 0 1 3 15.75V9.456c0-1.081.768-2.015 1.837-2.175a48.041 48.041 0 0 1 1.913-.247m10.5 0a48.536 48.536 0 0 0-10.5 0m10.5 0V3.375c0-.621-.504-1.125-1.125-1.125h-8.25c-.621 0-1.125.504-1.125 1.125v3.659" />
            </svg>
            Exportar PDF
          </button>
        </div>
      </div>
    </header>

    <p v-if="cargando" class="text-ink-faint py-24 text-center text-sm">Cargando informe…</p>
    <p v-else-if="error" class="py-24 text-center text-sm text-red-700">{{ error }}</p>

    <div v-else-if="informe" class="mx-auto max-w-[1100px] px-5 py-8 print:max-w-none print:p-0">
      <div
        v-if="verificacion"
        class="sin-imprimir mb-4 rounded-[0.9rem] px-4 py-3 text-sm"
        :class="
          verificacion.valida
            ? 'bg-[var(--chip-informado-bg)] text-[var(--color-estado-informado)]'
            : 'bg-red-500/10 text-red-700'
        "
      >
        <template v-if="verificacion.valida">
          Firma válida. El contenido no cambió desde que se firmó.
        </template>
        <template v-else>{{ verificacion.motivo }}</template>
      </div>

      <HojaInforme
        :hospital-nombre="informe.hospitalNombre"
        :hospital-provincia="informe.hospitalProvincia"
        :hospital-municipio="informe.hospitalMunicipio"
        :paciente-nombre="informe.pacienteNombre"
        :paciente-documento="informe.pacienteDocumento"
        :paciente-sexo="informe.pacienteSexo"
        :paciente-fecha-nacimiento="informe.pacienteFechaNacimiento"
        :modalidad="informe.modalidad"
        :descripcion-estudio="informe.descripcionEstudio"
        :fecha-estudio="informe.fechaEstudio"
        :identificador-estudio="informe.studyInstanceUid"
        :radiologo-nombre="informe.radiologoNombre"
        :es-adenda="informe.esAdenda"
        :firmado-at="informe.firmadoAt"
        :firmante-nombre="informe.firmanteNombre ?? informe.radiologoNombre"
        :firmante-matricula="informe.firmanteMatricula"
        :firma-imagen="informe.firmaImagen"
        :codigo-verificacion="informe.hashContenido"
        :algoritmo-firma="informe.algoritmoFirma"
      >
        <!-- v-html sobre contenido saneado con la lista compartida de etiquetas. -->
        <div class="informe-prosa" v-html="cuerpo" />
      </HojaInforme>

    </div>
  </div>
</template>
