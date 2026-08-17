<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { isAxiosError } from 'axios'
import { api } from '@/services/api'
import { useToastStore } from '@/stores/toast'
import Modal from '@/components/Modal.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import { MODALIDADES, SECCIONES_ESTANDAR, type GuardarPlantilla, type Plantilla } from '@/types/plantilla'

const toasts = useToastStore()

const plantillas = ref<Plantilla[]>([])
const cargando = ref(true)
const error = ref<string | null>(null)
const busqueda = ref('')

const editorAbierto = ref(false)
const editandoId = ref<string | null>(null)
const guardando = ref(false)
const errorForm = ref<string | null>(null)
const aBorrar = ref<Plantilla | null>(null)

const nombre = ref('')
const modalidad = ref('')
const region = ref('')
const descripcion = ref('')
const favorita = ref(false)
const secciones = ref<{ titulo: string; contenido: string }[]>([])

async function cargar() {
  cargando.value = true
  error.value = null
  try {
    const { data } = await api.get<Plantilla[]>('/plantillas')
    plantillas.value = data
  } catch {
    error.value = 'No se pudieron cargar las plantillas.'
  } finally {
    cargando.value = false
  }
}

onMounted(cargar)

const visibles = computed(() => {
  const texto = busqueda.value.trim().toLowerCase()
  if (!texto) return plantillas.value

  return plantillas.value.filter(
    (p) =>
      p.nombre.toLowerCase().includes(texto) ||
      (p.regionAnatomica ?? '').toLowerCase().includes(texto) ||
      (p.modalidad ?? '').toLowerCase().includes(texto),
  )
})

function nueva() {
  editandoId.value = null
  nombre.value = ''
  modalidad.value = ''
  region.value = ''
  descripcion.value = ''
  favorita.value = false
  secciones.value = SECCIONES_ESTANDAR.map((s) => ({ ...s }))
  errorForm.value = null
  editorAbierto.value = true
}

function editar(p: Plantilla) {
  editandoId.value = p.id
  nombre.value = p.nombre
  modalidad.value = p.modalidad ?? ''
  region.value = p.regionAnatomica ?? ''
  descripcion.value = p.descripcion ?? ''
  favorita.value = p.favorita
  secciones.value = [...p.secciones]
    .sort((a, b) => a.orden - b.orden)
    .map((s) => ({ titulo: s.titulo, contenido: s.contenido ?? '' }))
  errorForm.value = null
  editorAbierto.value = true
}

function duplicar(p: Plantilla) {
  editar(p)
  editandoId.value = null
  nombre.value = `${p.nombre} (copia)`
}

function agregarSeccion() {
  secciones.value.push({ titulo: '', contenido: '' })
}

function quitarSeccion(i: number) {
  secciones.value.splice(i, 1)
}

function mover(i: number, delta: number) {
  const destino = i + delta
  if (destino < 0 || destino >= secciones.value.length) return
  const copia = [...secciones.value]
  ;[copia[i], copia[destino]] = [copia[destino], copia[i]]
  secciones.value = copia
}

async function guardar() {
  errorForm.value = null

  if (!nombre.value.trim()) {
    errorForm.value = 'Ponele un nombre a la plantilla.'
    return
  }
  if (secciones.value.some((s) => !s.titulo.trim())) {
    errorForm.value = 'Todas las secciones necesitan un título.'
    return
  }

  const cuerpo: GuardarPlantilla = {
    nombre: nombre.value.trim(),
    modalidad: modalidad.value || null,
    regionAnatomica: region.value.trim() || null,
    descripcion: descripcion.value.trim() || null,
    favorita: favorita.value,
    secciones: secciones.value.map((s, i) => ({
      titulo: s.titulo.trim(),
      contenido: s.contenido.trim() || null,
      orden: i,
    })),
  }

  guardando.value = true
  try {
    if (editandoId.value) {
      await api.put(`/plantillas/${editandoId.value}`, cuerpo)
      toasts.exito('Plantilla actualizada.')
    } else {
      await api.post('/plantillas', cuerpo)
      toasts.exito('Plantilla creada. Ya aparece al redactar un informe.')
    }
    editorAbierto.value = false
    await cargar()
  } catch (e) {
    const mensaje: string =
      isAxiosError(e) && e.response?.data?.detail ? e.response.data.detail : 'No se pudo guardar la plantilla.'
    errorForm.value = mensaje
    toasts.error(mensaje)
  } finally {
    guardando.value = false
  }
}

async function confirmarBorrado() {
  const p = aBorrar.value
  aBorrar.value = null
  if (!p) return

  try {
    await api.delete(`/plantillas/${p.id}`)
    await cargar()
    toasts.exito(`Se eliminó «${p.nombre}».`)
  } catch {
    toasts.error('No se pudo eliminar la plantilla.')
  }
}

function etiquetaModalidad(codigo: string | null): string {
  if (!codigo) return 'Todas'
  return MODALIDADES.find((m) => m.valor === codigo)?.etiqueta.split(' — ')[0] ?? codigo
}
</script>

<template>
  <div class="stagger space-y-7">
    <div class="flex flex-wrap items-end justify-between gap-4">
      <div>
        <RouterLink to="/configuracion/radiologo" class="meta-label hover:text-ink transition-colors">
          ← Configuración
        </RouterLink>
        <h1 class="display mt-1.5 text-3xl sm:text-4xl">Plantillas de informe</h1>
        <p class="text-ink-soft mt-2 max-w-xl text-sm leading-relaxed">
          Estructuras que reusás al redactar. Aparecen como opción al empezar un informe, filtradas por la modalidad
          del estudio.
        </p>
      </div>
      <button type="button" class="btn-ink" @click="nueva">
        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
        </svg>
        Nueva plantilla
      </button>
    </div>

    <div v-if="plantillas.length > 0" class="glass p-4">
      <input v-model="busqueda" type="search" placeholder="Buscar por nombre, región o modalidad…" class="field" />
    </div>

    <div v-if="cargando" class="text-ink-faint py-16 text-center text-sm">Cargando plantillas…</div>

    <div
      v-else-if="plantillas.length === 0"
      class="glass flex flex-col items-center gap-4 px-6 py-16 text-center"
    >
      <span class="avatar-ring h-14 w-14">
        <span>
          <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z"
            />
          </svg>
        </span>
      </span>
      <div>
        <p class="text-base font-medium">Todavía no tenés plantillas</p>
        <p class="text-ink-soft mt-1.5 max-w-md text-sm leading-relaxed">
          Una plantilla arranca con las cinco secciones del informe estándar — datos, técnica, hallazgos, impresión
          y recomendaciones — y las ajustás a tu forma de trabajar.
        </p>
      </div>
      <button type="button" class="btn-ink" @click="nueva">Crear la primera</button>
    </div>

    <div v-else class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
      <article
        v-for="p in visibles"
        :key="p.id"
        class="glass group relative flex flex-col overflow-hidden p-5 transition-shadow hover:shadow-lg"
      >
        <div
          v-if="p.favorita"
          class="absolute inset-x-0 top-0 h-1 bg-gradient-to-r from-[var(--color-coral)] via-[var(--color-lilac)] to-[var(--color-aqua)]"
        />

        <div class="flex items-start justify-between gap-3">
          <div class="min-w-0">
            <p class="truncate text-base font-medium">{{ p.nombre }}</p>
            <p v-if="p.regionAnatomica" class="text-ink-faint mt-0.5 truncate text-xs">{{ p.regionAnatomica }}</p>
          </div>
          <span class="chip flex-none" :class="p.modalidad ? 'chip-informe' : 'chip-neutro'">
            {{ etiquetaModalidad(p.modalidad) }}
          </span>
        </div>

        <p v-if="p.descripcion" class="text-ink-soft mt-3 line-clamp-2 text-sm leading-relaxed">
          {{ p.descripcion }}
        </p>

        <ol class="mt-4 space-y-1.5">
          <li
            v-for="s in [...p.secciones].sort((a, b) => a.orden - b.orden).slice(0, 5)"
            :key="s.orden"
            class="text-ink-soft flex items-center gap-2 text-xs"
          >
            <span class="bg-ink/10 flex h-4 w-4 flex-none items-center justify-center rounded text-[0.625rem] tabular-nums">
              {{ s.orden + 1 }}
            </span>
            <span class="truncate">{{ s.titulo }}</span>
          </li>
          <li v-if="p.secciones.length > 5" class="text-ink-faint pl-6 text-xs">
            +{{ p.secciones.length - 5 }} más
          </li>
        </ol>

        <div class="mt-auto flex items-center justify-between gap-2 pt-5">
          <span class="text-ink-faint text-xs">
            {{ p.vecesUsada === 0 ? 'Sin usar' : `Usada ${p.vecesUsada} ${p.vecesUsada === 1 ? 'vez' : 'veces'}` }}
          </span>
          <div class="flex gap-1.5 opacity-0 transition-opacity group-hover:opacity-100 focus-within:opacity-100">
            <button type="button" class="btn-ghost !px-2.5 !py-1 !text-xs" @click="duplicar(p)">Duplicar</button>
            <button type="button" class="btn-ghost !px-2.5 !py-1 !text-xs" @click="editar(p)">Editar</button>
            <button type="button" class="btn-ghost !px-2.5 !py-1 !text-xs" @click="aBorrar = p">Eliminar</button>
          </div>
        </div>
      </article>

      <p v-if="visibles.length === 0" class="text-ink-faint col-span-full py-12 text-center text-sm">
        Ninguna plantilla coincide con «{{ busqueda }}».
      </p>
    </div>

    <p v-if="error" class="text-sm text-red-700">{{ error }}</p>

    <Modal
      :abierto="editorAbierto"
      :titulo="editandoId ? 'Editar plantilla' : 'Nueva plantilla'"
      subtitulo="Las secciones se aplican en este orden cuando la uses en un informe."
      ancho="xl"
      @cerrar="editorAbierto = false"
    >
      <div class="space-y-6">
        <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div class="sm:col-span-2">
            <label class="meta-label mb-1.5 block" for="p-nombre">Nombre</label>
            <input
              id="p-nombre"
              v-model="nombre"
              type="text"
              maxlength="200"
              placeholder="TC de abdomen sin contraste"
              class="field"
            />
          </div>
          <div>
            <label class="meta-label mb-1.5 block" for="p-modalidad">Modalidad</label>
            <select id="p-modalidad" v-model="modalidad" class="field">
              <option v-for="m in MODALIDADES" :key="m.valor" :value="m.valor">{{ m.etiqueta }}</option>
            </select>
          </div>
          <div>
            <label class="meta-label mb-1.5 block" for="p-region">Región anatómica</label>
            <input id="p-region" v-model="region" type="text" maxlength="120" placeholder="Abdomen y pelvis" class="field" />
          </div>
          <div class="sm:col-span-2">
            <label class="meta-label mb-1.5 block" for="p-desc">Descripción</label>
            <input id="p-desc" v-model="descripcion" type="text" maxlength="500" class="field" />
          </div>
        </div>

        <label class="flex cursor-pointer items-center gap-2.5 text-sm">
          <input v-model="favorita" type="checkbox" class="h-4 w-4 rounded" />
          Marcar como favorita — aparece primero al elegir plantilla
        </label>

        <div>
          <div class="mb-3 flex items-center justify-between">
            <p class="meta-label">Secciones</p>
            <button type="button" class="btn-ghost !px-3 !py-1.5 !text-xs" @click="agregarSeccion">
              Agregar sección
            </button>
          </div>

          <div class="space-y-3">
            <div
              v-for="(s, i) in secciones"
              :key="i"
              class="rounded-2xl border border-[var(--color-borde)] bg-[var(--color-campo)] p-4"
            >
              <div class="flex items-center gap-2">
                <span
                  class="bg-ink text-[var(--color-sobre-tinta)] flex h-6 w-6 flex-none items-center justify-center rounded-full text-xs tabular-nums"
                >
                  {{ i + 1 }}
                </span>
                <input
                  v-model="s.titulo"
                  type="text"
                  maxlength="120"
                  placeholder="Título de la sección"
                  class="field !py-1.5 flex-1 font-medium"
                />
                <button
                  type="button"
                  class="btn-orb !h-7 !w-7"
                  :disabled="i === 0"
                  title="Subir"
                  @click="mover(i, -1)"
                >
                  <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="m4.5 15.75 7.5-7.5 7.5 7.5" />
                  </svg>
                </button>
                <button
                  type="button"
                  class="btn-orb !h-7 !w-7"
                  :disabled="i === secciones.length - 1"
                  title="Bajar"
                  @click="mover(i, 1)"
                >
                  <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="m19.5 8.25-7.5 7.5-7.5-7.5" />
                  </svg>
                </button>
                <button type="button" class="btn-orb !h-7 !w-7" title="Quitar" @click="quitarSeccion(i)">
                  <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>

              <textarea
                v-model="s.contenido"
                rows="2"
                placeholder="Texto por defecto (opcional) — por ejemplo, los hallazgos normales que después ajustás"
                class="field mt-2.5 resize-y text-sm"
              />
            </div>

            <p v-if="secciones.length === 0" class="text-ink-faint py-6 text-center text-sm">
              Agregá al menos una sección.
            </p>
          </div>
        </div>

        <p v-if="errorForm" class="rounded-xl bg-red-500/10 px-3 py-2 text-sm text-red-700">{{ errorForm }}</p>

        <div class="flex justify-end gap-3">
          <button type="button" class="btn-ghost" @click="editorAbierto = false">Cancelar</button>
          <button type="button" :disabled="guardando" class="btn-ink" @click="guardar">
            {{ guardando ? 'Guardando…' : editandoId ? 'Guardar cambios' : 'Crear plantilla' }}
          </button>
        </div>
      </div>
    </Modal>

    <ConfirmDialog
      :abierto="aBorrar !== null"
      titulo="Eliminar plantilla"
      :mensaje="`«${aBorrar?.nombre ?? ''}» deja de aparecer al redactar. Los informes que ya salieron de ella no se tocan.`"
      texto-confirmar="Eliminar"
      tono="peligro"
      @confirmar="confirmarBorrado"
      @cancelar="aBorrar = null"
    />
  </div>
</template>
