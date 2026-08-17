<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { isAxiosError } from 'axios'
import { api } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import EditorInforme from '@/components/EditorInforme.vue'
import SelectorPlantilla from '@/components/SelectorPlantilla.vue'
import PadFirma from '@/components/PadFirma.vue'
import Modal from '@/components/Modal.vue'
import type { Estudio } from '@/types/estudio'
import type { Informe } from '@/types/informe'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const toasts = useToastStore()

const estudioId = route.params.id as string
const informeIdParam = route.query.informe as string | undefined
const esAdenda = route.query.adenda === '1'

const estudio = ref<Estudio | null>(null)
const informe = ref<Informe | null>(null)
const contenido = ref('')
const cargando = ref(true)
const guardando = ref(false)
const error = ref<string | null>(null)

const modalFirma = ref(false)
const trazoFirma = ref<string | null>(null)

const soloLectura = computed(() => informe.value?.estado === 'Firmado')

async function cargar() {
  cargando.value = true
  try {
    const [e, is] = await Promise.all([
      api.get<Estudio>(`/estudios/${estudioId}`),
      api.get<Informe[]>(`/estudios/${estudioId}/informes`),
    ])
    estudio.value = e.data

    // Sin ?informe= se abre el último documento del estudio, sea borrador o firmado:
    // buscar solo borradores dejaba la vista con el objeto previo apenas se firmaba.
    const existente = informeIdParam
      ? is.data.find((i) => i.id === informeIdParam)
      : is.data[is.data.length - 1]

    if (existente) {
      informe.value = existente
      contenido.value = normalizar(existente.contenido)
    }
  } catch {
    error.value = 'No se pudo cargar el estudio.'
  } finally {
    cargando.value = false
  }
}

// Los informes viejos son texto plano: se envuelven en párrafos para que el editor los muestre.
function normalizar(texto: string): string {
  if (texto.trimStart().startsWith('<')) return texto
  return texto
    .split(/\n{2,}/)
    .map((p) => `<p>${p.replace(/\n/g, '<br>')}</p>`)
    .join('')
}

onMounted(cargar)

function mensajeDeError(e: unknown, fallback: string): string {
  return isAxiosError(e) && (e.response?.data?.detail || e.response?.data?.message)
    ? (e.response.data.detail ?? e.response.data.message)
    : fallback
}

const vacio = computed(() => {
  const texto = contenido.value.replace(/<[^>]*>/g, '').trim()
  return texto.length === 0
})

async function guardarBorrador(avisar = true) {
  if (vacio.value) {
    toasts.error('El informe está vacío.')
    return
  }

  guardando.value = true
  try {
    if (informe.value) {
      await api.put(`/informes/${informe.value.id}`, { contenido: contenido.value })
    } else if (esAdenda) {
      const anterior = informeIdParam
      const { data } = await api.post<Informe>(`/informes/${anterior}/adenda`, { contenido: contenido.value })
      informe.value = data
    } else {
      const { data } = await api.post<Informe>(`/estudios/${estudioId}/informes`, { contenido: contenido.value })
      informe.value = data
    }
    if (avisar) toasts.exito('Borrador guardado.')
  } catch (e) {
    toasts.error(mensajeDeError(e, 'No se pudo guardar el informe.'))
    throw e
  } finally {
    guardando.value = false
  }
}

async function abrirFirma() {
  try {
    await guardarBorrador(false)
    modalFirma.value = true
  } catch {
    // guardarBorrador ya avisó del error.
  }
}

async function firmar() {
  if (!informe.value) return

  const trazo = trazoFirma.value
  modalFirma.value = false
  trazoFirma.value = null
  guardando.value = true

  const aviso = toasts.cargando('Firmando el informe…')
  try {
    // La respuesta ya trae la firma, el sello de tiempo y el estado: recargar la lista
    // no aportaba nada y podía no reencontrar el informe.
    const { data } = await api.post<Informe>(`/informes/${informe.value.id}/firmar`, { firmaImagen: trazo })
    informe.value = data
    toasts.exito('Informe firmado. Se le notificó a quien subió el estudio.')
  } catch (e) {
    toasts.error(mensajeDeError(e, 'No se pudo firmar el informe.'))
  } finally {
    toasts.cerrar(aviso)
    guardando.value = false
  }
}

function exportarPdf() {
  window.print()
}

const formatoFecha = new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
const formatoFechaHora = new Intl.DateTimeFormat('es-AR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})
</script>

<template>
  <div class="min-h-screen bg-[var(--color-lienzo)]">
    <!-- Barra de trabajo: no se imprime. -->
    <header class="sin-imprimir sticky top-0 z-20 border-b border-[var(--color-hairline)] bg-[var(--color-vidrio-solido)] backdrop-blur">
      <div class="mx-auto flex max-w-[1100px] flex-wrap items-center justify-between gap-3 px-5 py-3">
        <div class="flex items-center gap-3">
          <button type="button" class="btn-orb" title="Volver al estudio" @click="router.push(`/estudios/${estudioId}`)">
            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
            </svg>
          </button>
          <div>
            <p class="meta-label">{{ esAdenda ? 'Adenda' : 'Informe radiológico' }}</p>
            <p class="text-sm font-medium">{{ estudio?.pacienteNombre ?? '…' }}</p>
          </div>
          <span v-if="informe" class="chip" :class="soloLectura ? 'chip-informado' : 'chip-neutro'">
            {{ soloLectura ? 'Firmado' : 'Borrador' }}
          </span>
        </div>

        <div class="flex items-center gap-2">
          <button type="button" class="btn-ghost !py-2 !text-xs" @click="exportarPdf">
            <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" d="M6.72 13.829c-.24.03-.48.062-.72.096m.72-.096a42.415 42.415 0 0 1 10.56 0m-10.56 0L6.34 18m10.94-4.171c.24.03.48.062.72.096m-.72-.096L17.66 18m0 0 .229 2.523a1.125 1.125 0 0 1-1.12 1.227H7.231c-.662 0-1.18-.568-1.12-1.227L6.34 18m11.318 0h1.091A2.25 2.25 0 0 0 21 15.75V9.456c0-1.081-.768-2.015-1.837-2.175a48.055 48.055 0 0 0-1.913-.247M6.34 18H5.25A2.25 2.25 0 0 1 3 15.75V9.456c0-1.081.768-2.015 1.837-2.175a48.041 48.041 0 0 1 1.913-.247m10.5 0a48.536 48.536 0 0 0-10.5 0m10.5 0V3.375c0-.621-.504-1.125-1.125-1.125h-8.25c-.621 0-1.125.504-1.125 1.125v3.659" />
            </svg>
            Exportar PDF
          </button>
          <template v-if="!soloLectura">
            <button type="button" :disabled="guardando || vacio" class="btn-ghost !py-2 !text-xs" @click="guardarBorrador()">
              {{ guardando ? 'Guardando…' : 'Guardar borrador' }}
            </button>
            <button type="button" :disabled="guardando || vacio" class="btn-ink !py-2 !text-xs" @click="abrirFirma">
              Firmar
            </button>
          </template>
        </div>
      </div>
    </header>

    <p v-if="cargando" class="text-ink-faint py-24 text-center text-sm">Cargando…</p>
    <p v-else-if="error" class="py-24 text-center text-sm text-red-700">{{ error }}</p>

    <!-- La hoja: mismo layout en pantalla y en papel. -->
    <div v-else class="mx-auto max-w-[1100px] px-5 py-8">
      <div class="sin-imprimir mb-4">
        <SelectorPlantilla
          v-if="!soloLectura"
          :modalidad="estudio?.modalidad"
          @aplicar="(t) => (contenido = normalizar(t))"
        />
      </div>

      <article class="hoja">
        <header class="hoja-encabezado">
          <div class="flex items-start justify-between gap-6">
            <div>
              <p class="hoja-titulo">Informe radiológico</p>
              <p class="hoja-sub">{{ estudio?.hospitalNombre }}</p>
            </div>
            <div class="text-right">
              <p class="hoja-sub">Emitido</p>
              <p class="text-sm tabular-nums">
                {{ formatoFecha.format(informe?.firmadoAt ? new Date(informe.firmadoAt) : new Date()) }}
              </p>
            </div>
          </div>

          <dl class="hoja-datos">
            <div><dt>Paciente</dt><dd>{{ estudio?.pacienteNombre }}</dd></div>
            <div><dt>Documento</dt><dd>{{ estudio?.pacienteDocumento }}</dd></div>
            <div><dt>Modalidad</dt><dd>{{ estudio?.modalidad }}</dd></div>
            <div>
              <dt>Fecha del estudio</dt>
              <dd>{{ estudio ? formatoFecha.format(new Date(estudio.fechaEstudio)) : '' }}</dd>
            </div>
          </dl>
        </header>

        <div class="hoja-cuerpo">
          <EditorInforme v-model="contenido" :editable="!soloLectura" />
        </div>

        <!-- Pie con la firma: se repite en cada página al imprimir. -->
        <footer class="hoja-pie">
          <div class="hoja-firma">
            <img
              v-if="informe?.firmaImagen"
              :src="informe.firmaImagen"
              alt="Firma del radiólogo"
              class="hoja-firma-trazo"
            />
            <div v-else class="hoja-firma-linea" />

            <p class="hoja-firma-nombre">
              {{ informe?.firmanteNombre ?? auth.usuario?.nombreCompleto }}
            </p>
            <p class="hoja-firma-datos">
              <template v-if="informe?.firmanteMatricula ?? auth.usuario?.matricula">
                Matrícula {{ informe?.firmanteMatricula ?? auth.usuario?.matricula }}
              </template>
              <template v-else>Médico radiólogo</template>
            </p>
            <p v-if="informe?.firmadoAt" class="hoja-firma-datos">
              Firmado digitalmente el {{ formatoFechaHora.format(new Date(informe.firmadoAt)) }}
            </p>
            <p v-else class="hoja-firma-datos hoja-borrador">Borrador — sin firmar</p>
          </div>
        </footer>
      </article>
    </div>

    <Modal
      :abierto="modalFirma"
      titulo="Firmar el informe"
      subtitulo="Una vez firmado queda inmutable: cualquier corrección posterior va como adenda."
      @cerrar="((modalFirma = false), (trazoFirma = null))"
    >
      <PadFirma :nombre="auth.usuario?.nombreCompleto ?? ''" @cambio="(f) => (trazoFirma = f)" />

      <div class="mt-6 flex flex-wrap items-center justify-between gap-3">
        <p class="text-ink-faint text-xs">
          {{ trazoFirma ? 'Firma lista.' : 'Podés firmar sin trazo, pero el informe queda sin firma manuscrita.' }}
        </p>
        <div class="flex gap-3">
          <button type="button" class="btn-ghost" @click="((modalFirma = false), (trazoFirma = null))">Cancelar</button>
          <button type="button" :disabled="guardando" class="btn-ink" @click="firmar">Firmar</button>
        </div>
      </div>
    </Modal>
  </div>
</template>
