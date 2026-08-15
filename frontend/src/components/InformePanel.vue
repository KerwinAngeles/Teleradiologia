<script setup lang="ts">
import { computed, ref } from 'vue'
import { isAxiosError } from 'axios'
import { api } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import type { Estudio } from '@/types/estudio'
import type { Informe } from '@/types/informe'

const props = defineProps<{ estudio: Estudio; informes: Informe[] }>()
const emit = defineEmits<{ actualizar: [] }>()

const auth = useAuthStore()
const toasts = useToastStore()

// El historial viene por fecha: el último es el vigente.
const ultimoInforme = computed<Informe | null>(() => props.informes[props.informes.length - 1] ?? null)
const esRadiologoAsignado = computed(() => auth.usuario?.id === props.estudio.radiologoAsignadoId)
const puedeCrearBorradorInicial = computed(
  () => props.informes.length === 0 && esRadiologoAsignado.value && props.estudio.estado === 'EnInforme',
)
const puedeAgregarAdenda = computed(() => ultimoInforme.value?.estado === 'Firmado' && esRadiologoAsignado.value)

const enviando = ref(false)
const error = ref<string | null>(null)

const contenidoNuevo = ref('')
const editandoId = ref<string | null>(null)
const contenidoEdit = ref('')
const creandoAdenda = ref(false)
const contenidoAdenda = ref('')

function mensajeError(e: unknown, fallback: string): string {
  return isAxiosError(e) && e.response?.data?.message ? e.response.data.message : fallback
}

function reportarError(e: unknown, fallback: string) {
  const mensaje = mensajeError(e, fallback)
  error.value = mensaje
  toasts.error(mensaje)
}

async function crearBorradorInicial() {
  error.value = null
  enviando.value = true
  try {
    await api.post(`/estudios/${props.estudio.id}/informes`, { contenido: contenidoNuevo.value })
    contenidoNuevo.value = ''
    emit('actualizar')
    toasts.exito('Borrador guardado. Podés seguir editándolo hasta firmarlo.')
  } catch (e) {
    reportarError(e, 'No se pudo crear el informe.')
  } finally {
    enviando.value = false
  }
}

function empezarEdicion(informe: Informe) {
  editandoId.value = informe.id
  contenidoEdit.value = informe.contenido
  error.value = null
}

async function guardarEdicion() {
  if (!editandoId.value) return
  error.value = null
  enviando.value = true
  try {
    await api.put(`/informes/${editandoId.value}`, { contenido: contenidoEdit.value })
    editandoId.value = null
    emit('actualizar')
    toasts.exito('Cambios guardados en el borrador.')
  } catch (e) {
    reportarError(e, 'No se pudo guardar el informe.')
  } finally {
    enviando.value = false
  }
}

const informeAFirmar = ref<Informe | null>(null)

async function firmar() {
  const informe = informeAFirmar.value
  if (!informe) return

  informeAFirmar.value = null
  error.value = null
  enviando.value = true
  const avisoEnCurso = toasts.cargando('Firmando el informe y avisando al hospital…')
  try {
    await api.post(`/informes/${informe.id}/firmar`)
    emit('actualizar')
    toasts.exito('Informe firmado. Se le notificó por email a quien subió el estudio.')
  } catch (e) {
    reportarError(e, 'No se pudo firmar el informe.')
  } finally {
    toasts.cerrar(avisoEnCurso)
    enviando.value = false
  }
}

async function crearAdenda() {
  if (!ultimoInforme.value) return
  error.value = null
  enviando.value = true
  try {
    await api.post(`/informes/${ultimoInforme.value.id}/adenda`, { contenido: contenidoAdenda.value })
    contenidoAdenda.value = ''
    creandoAdenda.value = false
    emit('actualizar')
    toasts.exito('Adenda creada como borrador. Queda enlazada al informe original.')
  } catch (e) {
    reportarError(e, 'No se pudo crear la adenda.')
  } finally {
    enviando.value = false
  }
}

const formatoFechaHora = new Intl.DateTimeFormat('es-AR', { dateStyle: 'short', timeStyle: 'short' })
const areaTexto = 'field mt-3 min-h-[9rem] resize-y leading-relaxed'
</script>

<template>
  <section class="glass p-6">
    <div class="flex items-center justify-between">
      <div>
        <p class="meta-label">Lectura</p>
        <h2 class="display mt-1 text-xl">Informe</h2>
      </div>
      <span v-if="informes.length" class="meta-label tabular-nums">
        {{ informes.length }} {{ informes.length === 1 ? 'documento' : 'documentos' }}
      </span>
    </div>

    <p v-if="informes.length === 0 && !puedeCrearBorradorInicial" class="text-ink-faint mt-4 text-sm">
      Todavía no hay informe para este estudio.
    </p>

    <TransitionGroup name="fade-slide" tag="div" class="mt-5 space-y-4">
      <article
        v-for="(informe, i) in informes"
        :key="informe.id"
        class="rounded-[1.1rem] border border-[var(--color-hairline)] bg-white/55 p-4 transition-shadow hover:shadow-sm"
      >
        <div class="flex items-center justify-between gap-3">
          <p class="text-sm font-medium">{{ informe.esAdenda ? `Adenda ${i}` : 'Informe original' }}</p>
          <span class="chip" :class="informe.estado === 'Firmado' ? 'chip-informado' : 'chip-neutro'">
            {{ informe.estado === 'Firmado' ? 'Firmado' : 'Borrador' }}
          </span>
        </div>
        <p class="text-ink-faint mt-1 text-xs">
          {{ informe.radiologoNombre }} · {{ formatoFechaHora.format(new Date(informe.createdAt)) }}
          <template v-if="informe.firmadoAt">
            · firmado {{ formatoFechaHora.format(new Date(informe.firmadoAt)) }}
          </template>
        </p>

        <template v-if="editandoId === informe.id">
          <textarea v-model="contenidoEdit" :class="areaTexto" />
          <div class="mt-3 flex gap-2">
            <button type="button" :disabled="enviando" class="btn-ink !py-2 !text-xs" @click="guardarEdicion">
              Guardar
            </button>
            <button type="button" class="btn-ghost !py-2 !text-xs" @click="editandoId = null">Cancelar</button>
          </div>
        </template>
        <p v-else class="text-ink-soft mt-3 text-sm leading-relaxed whitespace-pre-wrap">{{ informe.contenido }}</p>

        <div
          v-if="editandoId !== informe.id && informe.estado === 'Borrador' && informe.radiologoId === auth.usuario?.id"
          class="mt-4 flex gap-2"
        >
          <button type="button" class="btn-ghost !py-2 !text-xs" @click="empezarEdicion(informe)">Editar</button>
          <button type="button" :disabled="enviando" class="btn-ink !py-2 !text-xs" @click="informeAFirmar = informe">
            Firmar
          </button>
        </div>
      </article>
    </TransitionGroup>

    <div v-if="puedeCrearBorradorInicial" class="mt-5">
      <p class="meta-label">Redactar hallazgos</p>
      <textarea v-model="contenidoNuevo" :class="areaTexto" placeholder="Hallazgos, impresión diagnóstica…" />
      <button
        type="button"
        :disabled="enviando || !contenidoNuevo.trim()"
        class="btn-ink mt-3 !py-2 !text-xs"
        @click="crearBorradorInicial"
      >
        Guardar borrador
      </button>
    </div>

    <div v-if="puedeAgregarAdenda" class="mt-5 border-t border-[var(--color-hairline)] pt-5">
      <button v-if="!creandoAdenda" type="button" class="btn-ghost !py-2 !text-xs" @click="creandoAdenda = true">
        + Agregar adenda
      </button>
      <template v-else>
        <p class="meta-label">Nueva adenda</p>
        <textarea v-model="contenidoAdenda" :class="areaTexto" placeholder="Corrección o ampliación del informe…" />
        <div class="mt-3 flex gap-2">
          <button
            type="button"
            :disabled="enviando || !contenidoAdenda.trim()"
            class="btn-ink !py-2 !text-xs"
            @click="crearAdenda"
          >
            Guardar adenda
          </button>
          <button type="button" class="btn-ghost !py-2 !text-xs" @click="creandoAdenda = false">Cancelar</button>
        </div>
      </template>
    </div>

    <p v-if="error" class="mt-4 text-sm text-red-700">{{ error }}</p>

    <ConfirmDialog
      :abierto="informeAFirmar !== null"
      titulo="Firmar el informe"
      mensaje="Una vez firmado, el informe queda inmutable: cualquier corrección posterior tendrá que ir como adenda. Además se le notifica por email a quien subió el estudio."
      texto-confirmar="Firmar"
      texto-cancelar="Revisar de nuevo"
      tono="peligro"
      @confirmar="firmar"
      @cancelar="informeAFirmar = null"
    />
  </section>
</template>
