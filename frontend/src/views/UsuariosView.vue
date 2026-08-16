<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { isAxiosError } from 'axios'
import { api } from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import Paginacion from '@/components/Paginacion.vue'
import { useDebounce } from '@/composables/useDebounce'
import type { EstadoAcceso, Rol, Usuario } from '@/types/auth'
import type { PagedResult } from '@/types/pagina'

const auth = useAuthStore()
const toasts = useToastStore()

const usuarios = ref<Usuario[]>([])
const cargando = ref(true)
const error = ref<string | null>(null)
const procesando = ref<string | null>(null)

const filtro = ref<EstadoAcceso | 'Todos'>('Todos')
const fRol = ref<Rol | ''>('')
const fTexto = ref('')
const textoDebounced = useDebounce(fTexto)

const pagina = ref(1)
const tamanoPagina = ref(20)
const total = ref(0)
const pendientes = ref(0)

const aprobando = ref<Usuario | null>(null)
const rolElegido = ref<Rol>('Radiologo')

const decidiendo = ref<{ usuario: Usuario; accion: 'rechazar' | 'suspender' } | null>(null)
const motivo = ref('')

async function cargar() {
  cargando.value = true
  error.value = null
  try {
    const { data } = await api.get<PagedResult<Usuario>>('/usuarios', {
      params: {
        pageNumber: pagina.value,
        pageSize: tamanoPagina.value,
        estado: filtro.value === 'Todos' ? undefined : filtro.value,
        rol: fRol.value || undefined,
        texto: textoDebounced.value.trim() || undefined,
      },
    })
    usuarios.value = data.items
    total.value = data.totalCount
  } catch {
    error.value = 'No se pudieron cargar los usuarios.'
  } finally {
    cargando.value = false
  }
}

// El contador de pendientes es del total, no de la página visible.
async function cargarPendientes() {
  try {
    const { data } = await api.get<PagedResult<Usuario>>('/usuarios', {
      params: { estado: 'Pendiente', pageSize: 1 },
    })
    pendientes.value = data.totalCount
  } catch {
    pendientes.value = 0
  }
}

watch([filtro, fRol, textoDebounced], () => {
  pagina.value = 1
  cargar()
})

watch(pagina, cargar)

onMounted(() => {
  cargar()
  cargarPendientes()
})

const visibles = computed(() => usuarios.value)

function mensajeDeError(e: unknown, fallback: string): string {
  return isAxiosError(e) && e.response?.data?.detail ? e.response.data.detail : fallback
}

async function ejecutar(usuario: Usuario, ruta: string, cuerpo: unknown, exito: string) {
  procesando.value = usuario.id
  try {
    await api.post(`/usuarios/${usuario.id}/${ruta}`, cuerpo)
    await Promise.all([cargar(), cargarPendientes()])
    toasts.exito(exito)
  } catch (e) {
    toasts.error(mensajeDeError(e, 'No se pudo completar la acción.'))
  } finally {
    procesando.value = null
  }
}

function abrirAprobar(usuario: Usuario) {
  rolElegido.value = usuario.rol === 'Admin' ? 'Admin' : 'Radiologo'
  aprobando.value = usuario
}

async function confirmarAprobar() {
  const usuario = aprobando.value
  aprobando.value = null
  if (!usuario) return

  await ejecutar(
    usuario,
    'aprobar',
    { rol: rolElegido.value },
    `${usuario.nombreCompleto} ya puede entrar como ${rolLabel[rolElegido.value]}.`,
  )
}

async function confirmarDecision() {
  const pendiente = decidiendo.value
  const texto = motivo.value.trim()
  decidiendo.value = null
  motivo.value = ''
  if (!pendiente) return

  const { usuario, accion } = pendiente
  await ejecutar(
    usuario,
    accion,
    { motivo: texto || null },
    accion === 'rechazar'
      ? `Se rechazó la solicitud de ${usuario.nombreCompleto}.`
      : `Se suspendió el acceso de ${usuario.nombreCompleto}.`,
  )
}

async function reactivar(usuario: Usuario) {
  await ejecutar(usuario, 'reactivar', {}, `${usuario.nombreCompleto} recuperó el acceso.`)
}

const rolLabel: Record<Rol, string> = {
  Tecnico: 'Técnico',
  Radiologo: 'Radiólogo',
  Admin: 'Admin',
}

const estadoChip: Record<EstadoAcceso, string> = {
  Pendiente: 'chip-pendiente',
  Aprobado: 'chip-informado',
  Rechazado: 'chip-neutro',
  Suspendido: 'chip-neutro',
}

const filtros: (EstadoAcceso | 'Todos')[] = ['Todos', 'Pendiente', 'Aprobado', 'Suspendido', 'Rechazado']

function inicial(nombre: string) {
  return nombre.charAt(0).toUpperCase()
}

const formatoFecha = new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
</script>

<template>
  <div class="stagger space-y-7">
    <div class="flex flex-wrap items-end justify-between gap-4">
      <div>
        <p class="meta-label">Administración</p>
        <h1 class="display mt-1.5 text-3xl sm:text-4xl">Usuarios</h1>
        <p class="text-ink-soft mt-2 text-sm">
          Las cuentas se crean por registro público y vos decidís quién entra y con qué rol.
        </p>
      </div>
      <div v-if="pendientes > 0" class="chip chip-pendiente">
        {{ pendientes }} esperando aprobación
      </div>
    </div>

    <div class="glass flex flex-wrap items-center gap-3 p-4">
      <div class="relative min-w-[220px] flex-1">
        <input v-model="fTexto" type="search" placeholder="Nombre o email…" class="field" />
      </div>
      <select v-model="fRol" class="field !w-auto">
        <option value="">Todos los roles</option>
        <option value="Tecnico">Técnico</option>
        <option value="Radiologo">Radiólogo</option>
        <option value="Admin">Admin</option>
      </select>
    </div>

    <div class="flex flex-wrap gap-2">
      <button
        v-for="f in filtros"
        :key="f"
        type="button"
        class="chip transition-colors"
        :class="filtro === f ? 'chip-informe' : 'chip-neutro'"
        @click="filtro = f"
      >
        {{ f }}
      </button>
    </div>

    <div class="glass overflow-hidden">
      <div class="overflow-x-auto">
        <table class="min-w-full">
          <thead>
            <tr class="border-b border-[var(--color-hairline)]">
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Nombre</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Email</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Rol</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Estado</th>
              <th class="meta-label px-5 py-3.5 text-left font-semibold">Solicitud</th>
              <th class="px-5 py-3.5"></th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="usuario in visibles"
              :key="usuario.id"
              class="border-b border-[var(--color-hairline)] transition-colors last:border-0 hover:bg-white/55"
            >
              <td class="px-5 py-3.5">
                <div class="flex items-center gap-3">
                  <span class="avatar-ring h-9 w-9 flex-none">
                    <span class="text-xs font-semibold">{{ inicial(usuario.nombreCompleto) }}</span>
                  </span>
                  <span class="text-sm font-medium">{{ usuario.nombreCompleto }}</span>
                </div>
              </td>
              <td class="text-ink-soft px-5 py-3.5 text-sm">{{ usuario.email }}</td>
              <td class="text-ink-soft px-5 py-3.5 text-sm">
                {{ usuario.estadoAcceso === 'Pendiente' ? '—' : rolLabel[usuario.rol] }}
              </td>
              <td class="px-5 py-3.5">
                <span class="chip" :class="estadoChip[usuario.estadoAcceso]">{{ usuario.estadoAcceso }}</span>
                <p v-if="usuario.motivoDecision" class="text-ink-faint mt-1 text-xs">{{ usuario.motivoDecision }}</p>
              </td>
              <td class="text-ink-soft px-5 py-3.5 text-sm tabular-nums">
                {{ formatoFecha.format(new Date(usuario.createdAt)) }}
              </td>
              <td class="px-5 py-3.5">
                <div class="flex items-center justify-end gap-2">
                  <template v-if="usuario.estadoAcceso === 'Pendiente'">
                    <button
                      type="button"
                      :disabled="procesando === usuario.id"
                      class="btn-ink !px-3.5 !py-1.5 !text-xs"
                      @click="abrirAprobar(usuario)"
                    >
                      Aprobar
                    </button>
                    <button
                      type="button"
                      :disabled="procesando === usuario.id"
                      class="btn-ghost !px-3.5 !py-1.5 !text-xs"
                      @click="decidiendo = { usuario, accion: 'rechazar' }"
                    >
                      Rechazar
                    </button>
                  </template>

                  <button
                    v-else-if="usuario.estadoAcceso === 'Aprobado' && usuario.id !== auth.usuario?.id"
                    type="button"
                    :disabled="procesando === usuario.id"
                    class="btn-ghost !px-3.5 !py-1.5 !text-xs"
                    @click="decidiendo = { usuario, accion: 'suspender' }"
                  >
                    Suspender
                  </button>

                  <button
                    v-else-if="usuario.estadoAcceso === 'Suspendido'"
                    type="button"
                    :disabled="procesando === usuario.id"
                    class="btn-ink !px-3.5 !py-1.5 !text-xs"
                    @click="reactivar(usuario)"
                  >
                    Reactivar
                  </button>

                  <button
                    v-else-if="usuario.estadoAcceso === 'Rechazado'"
                    type="button"
                    :disabled="procesando === usuario.id"
                    class="btn-ghost !px-3.5 !py-1.5 !text-xs"
                    @click="abrirAprobar(usuario)"
                  >
                    Dar acceso
                  </button>
                </div>
              </td>
            </tr>
            <tr v-if="cargando">
              <td colspan="6" class="text-ink-faint px-5 py-12 text-center text-sm">Cargando usuarios…</td>
            </tr>
            <tr v-else-if="visibles.length === 0">
              <td colspan="6" class="text-ink-faint px-5 py-12 text-center text-sm">
                No hay usuarios en este estado.
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <Paginacion :pagina="pagina" :tamano-pagina="tamanoPagina" :total="total" @cambiar="(p) => (pagina = p)" />
    </div>

    <p v-if="error" class="text-sm text-red-700">{{ error }}</p>

    <ConfirmDialog
      :abierto="aprobando !== null"
      titulo="Habilitar acceso"
      :mensaje="`Vas a darle acceso a ${aprobando?.nombreCompleto ?? ''}. Elegí con qué rol va a entrar.`"
      textoConfirmar="Aprobar"
      @confirmar="confirmarAprobar"
      @cancelar="aprobando = null"
    >
      <label class="meta-label mb-1.5 block" for="rol-aprobar">Rol</label>
      <select id="rol-aprobar" v-model="rolElegido" class="field">
        <option value="Tecnico">Técnico — sube estudios</option>
        <option value="Radiologo">Radiólogo — informa estudios</option>
        <option value="Admin">Admin — administra usuarios</option>
      </select>
    </ConfirmDialog>

    <ConfirmDialog
      :abierto="decidiendo !== null"
      :titulo="decidiendo?.accion === 'rechazar' ? 'Rechazar solicitud' : 'Suspender acceso'"
      :mensaje="
        decidiendo?.accion === 'rechazar'
          ? `${decidiendo?.usuario.nombreCompleto} no va a poder entrar. Se le avisa por email.`
          : `${decidiendo?.usuario.nombreCompleto} pierde el acceso hasta que lo reactives.`
      "
      :textoConfirmar="decidiendo?.accion === 'rechazar' ? 'Rechazar' : 'Suspender'"
      tono="peligro"
      @confirmar="confirmarDecision"
      @cancelar="((decidiendo = null), (motivo = ''))"
    >
      <label class="meta-label mb-1.5 block" for="motivo">Motivo (opcional)</label>
      <input id="motivo" v-model="motivo" type="text" maxlength="500" class="field" placeholder="Se le incluye en el email" />
    </ConfirmDialog>
  </div>
</template>
