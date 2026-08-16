<script setup lang="ts">
import { computed, ref } from 'vue'
import { isAxiosError } from 'axios'
import { api } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import Modal from '@/components/Modal.vue'
import PadFirma from '@/components/PadFirma.vue'
import type { Estudio } from '@/types/estudio'
import type { Informe, VerificacionFirma } from '@/types/informe'

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
const trazoFirma = ref<string | null>(null)

const verificaciones = ref<Record<string, VerificacionFirma>>({})
const verificando = ref<string | null>(null)

async function verificarFirma(informe: Informe) {
  verificando.value = informe.id
  try {
    const { data } = await api.get<VerificacionFirma>(`/informes/${informe.id}/verificacion`)
    verificaciones.value = { ...verificaciones.value, [informe.id]: data }
    if (data.valida) {
      toasts.exito('Firma válida: el contenido es el que se firmó.')
    } else {
      toasts.error(data.motivo ?? 'La firma no pudo validarse.')
    }
  } catch {
    toasts.error('No se pudo verificar la firma.')
  } finally {
    verificando.value = null
  }
}

function resumen(hash: string | null): string {
  if (!hash) return '—'
  return hash.length <= 16 ? hash : `${hash.slice(0, 8)}…${hash.slice(-8)}`
}

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

  const trazo = trazoFirma.value

  informeAFirmar.value = null
  trazoFirma.value = null
  error.value = null
  enviando.value = true
  const avisoEnCurso = toasts.cargando('Firmando el informe y avisando al hospital…')
  try {
    await api.post(`/informes/${informe.id}/firmar`, { firmaImagen: trazo })
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
        class="rounded-[1.1rem] border border-[var(--color-hairline)] bg-[var(--color-campo)] p-4 transition-shadow hover:shadow-sm"
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
          v-if="informe.estado === 'Firmado' && informe.hashContenido"
          class="mt-4 rounded-[0.9rem] border border-[var(--color-hairline)] bg-[var(--color-campo)] p-3.5"
        >
          <div class="flex flex-wrap items-start justify-between gap-3">
            <div class="flex items-start gap-2.5">
              <svg
                class="mt-0.5 h-4 w-4 flex-none text-[var(--color-estado-informado)]"
                fill="none"
                viewBox="0 0 24 24"
                stroke-width="1.8"
                stroke="currentColor"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  d="M9 12.75 11.25 15 15 9.75M21 12c0 1.268-.63 2.39-1.593 3.068a3.745 3.745 0 0 1-1.043 3.296 3.745 3.745 0 0 1-3.296 1.043A3.745 3.745 0 0 1 12 21c-1.268 0-2.39-.63-3.068-1.593a3.746 3.746 0 0 1-3.296-1.043 3.745 3.745 0 0 1-1.043-3.296A3.745 3.745 0 0 1 3 12c0-1.268.63-2.39 1.593-3.068a3.745 3.745 0 0 1 1.043-3.296 3.746 3.746 0 0 1 3.296-1.043A3.746 3.746 0 0 1 12 3c1.268 0 2.39.63 3.068 1.593a3.746 3.746 0 0 1 3.296 1.043 3.746 3.746 0 0 1 1.043 3.296A3.745 3.745 0 0 1 21 12Z"
                />
              </svg>
              <div>
                <p class="text-sm font-medium">{{ informe.firmanteNombre ?? informe.radiologoNombre }}</p>
                <p class="text-ink-faint text-xs">
                  <template v-if="informe.firmanteMatricula">Matrícula {{ informe.firmanteMatricula }} · </template>
                  Firmado digitalmente el {{ formatoFechaHora.format(new Date(informe.firmadoAt!)) }}
                </p>
                <p class="text-ink-faint mt-1 font-mono text-[0.6875rem]">
                  {{ informe.algoritmoFirma }} · {{ resumen(informe.hashContenido) }}
                </p>
                <img
                  v-if="informe.firmaImagen"
                  :src="informe.firmaImagen"
                  alt="Firma manuscrita"
                  class="mt-2 h-16 max-w-[260px] object-contain object-left"
                />
              </div>
            </div>

            <button
              type="button"
              :disabled="verificando === informe.id"
              class="btn-ghost !px-3 !py-1.5 !text-xs"
              @click="verificarFirma(informe)"
            >
              {{ verificando === informe.id ? 'Verificando…' : 'Verificar firma' }}
            </button>
          </div>

          <p
            v-if="verificaciones[informe.id]"
            class="mt-3 rounded-lg px-3 py-2 text-xs"
            :class="
              verificaciones[informe.id].valida
                ? 'bg-[var(--chip-informado-bg)] text-[var(--color-estado-informado)]'
                : 'bg-red-500/10 text-red-700'
            "
          >
            <template v-if="verificaciones[informe.id].valida">
              Firma válida. El contenido no cambió desde que se firmó.
            </template>
            <template v-else>
              {{ verificaciones[informe.id].motivo }}
            </template>
          </p>
        </div>

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

    <Modal
      :abierto="informeAFirmar !== null"
      titulo="Firmar el informe"
      subtitulo="Una vez firmado queda inmutable: cualquier corrección posterior va como adenda. Se le notifica por email a quien subió el estudio."
      @cerrar="((informeAFirmar = null), (trazoFirma = null))"
    >
      <PadFirma :nombre="auth.usuario?.nombreCompleto ?? ''" @cambio="(f) => (trazoFirma = f)" />

      <div class="mt-6 flex flex-wrap items-center justify-between gap-3">
        <p class="text-ink-faint text-xs">
          {{ trazoFirma ? 'Firma lista.' : 'Podés firmar sin trazo, pero el informe queda sin firma manuscrita.' }}
        </p>
        <div class="flex gap-3">
          <button type="button" class="btn-ghost" @click="((informeAFirmar = null), (trazoFirma = null))">
            Revisar de nuevo
          </button>
          <button type="button" :disabled="enviando" class="btn-ink" @click="firmar">Firmar</button>
        </div>
      </div>
    </Modal>
  </section>
</template>
