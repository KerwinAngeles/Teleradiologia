<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import LogoMark from '@/components/LogoMark.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import BotonTema from '@/components/BotonTema.vue'
import CampanaNotificaciones from '@/components/CampanaNotificaciones.vue'
import { useNotificacionesStore } from '@/stores/notificaciones'

const auth = useAuthStore()
const toasts = useToastStore()
const router = useRouter()
const route = useRoute()

// La estación de trabajo del visor pide todo el viewport; el resto de las
// pantallas sigue en la columna de lectura de 1400px.
const anchoCompleto = computed(() => route.meta.anchoCompleto === true)

const confirmandoSalida = ref(false)

const rolLabel: Record<string, string> = {
  Tecnico: 'Técnico',
  Radiologo: 'Radiólogo',
  Admin: 'Admin',
}

// `exacto` en Inicio porque su ruta es "/": con la coincidencia inclusiva de RouterLink
// se marcaría como activo en todas las pantallas.
const enlaces = computed(() => {
  const rol = auth.usuario?.rol

  return [
    { destino: '/', etiqueta: 'Inicio', exacto: true, visible: true },
    { destino: '/informes', etiqueta: 'Informes', exacto: false, visible: rol !== undefined },
    {
      destino: '/resultados',
      etiqueta: 'Resultados estudio',
      exacto: false,
      visible: rol === 'Tecnico' || rol === 'Admin',
    },
  ].filter((e) => e.visible)
})

// Cada rol tiene su propio panel: el del admin administra el sistema, el del radiólogo
// su espacio de trabajo (plantillas, tema). El orbe de la barra es el mismo.
const destinoConfiguracion = computed(() => {
  switch (auth.usuario?.rol) {
    case 'Admin':
      return '/configuracion'
    case 'Radiologo':
      return '/configuracion/radiologo'
    default:
      return null
  }
})
const recibeNotificaciones = computed(() => auth.usuario?.rol === 'Radiologo' || auth.usuario?.rol === 'Admin')

const notificaciones = useNotificacionesStore()

onMounted(() => {
  if (recibeNotificaciones.value) {
    notificaciones.cargarResumen()
    notificaciones.conectar()
  }
})

const inicial = computed(() => auth.usuario?.nombreCompleto.charAt(0).toUpperCase() ?? '?')

function salir() {
  confirmandoSalida.value = false
  notificaciones.desconectar()
  auth.logout()
  router.push({ name: 'login' })
  toasts.info('Cerraste sesión.')
}
</script>

<template>
  <div class="aurora" :class="anchoCompleto ? 'flex h-screen flex-col overflow-hidden' : 'min-h-screen'">
    <div
      :class="
        anchoCompleto
          ? 'flex min-h-0 flex-1 flex-col px-4 py-4 sm:px-6'
          : 'mx-auto max-w-[1400px] px-4 py-5 sm:px-8 sm:py-7'
      "
    >
        <!-- relative z-30: .glass trae backdrop-filter, que crea contexto de apilamiento. Sin z-index
             el header se pinta antes que <main> y el desplegable queda debajo del contenido. -->
        <header class="glass relative z-30 flex flex-wrap items-center justify-between gap-4 px-4 py-3 sm:px-5">
          <div class="flex items-center gap-6">
            <RouterLink to="/" class="flex items-center gap-2.5">
              <LogoMark />
              <span class="hidden text-sm font-semibold tracking-[0.18em] uppercase sm:block">Teleradiología</span>
            </RouterLink>

            <nav class="flex items-center gap-1">
              <RouterLink
                v-for="enlace in enlaces"
                :key="enlace.destino"
                :to="enlace.destino"
                class="nav-enlace"
                :active-class="enlace.exacto ? '' : 'nav-enlace-activo'"
                :exact-active-class="enlace.exacto ? 'nav-enlace-activo' : ''"
              >
                {{ enlace.etiqueta }}
              </RouterLink>
            </nav>
          </div>

          <div class="flex items-center gap-3">
            <div class="hidden text-right sm:block">
              <p class="text-sm leading-tight font-medium">{{ auth.usuario?.nombreCompleto }}</p>
              <p class="meta-label leading-tight">{{ rolLabel[auth.usuario?.rol ?? ''] }}</p>
            </div>
            <CampanaNotificaciones v-if="recibeNotificaciones" />
            <RouterLink
              v-if="destinoConfiguracion"
              :to="destinoConfiguracion"
              class="btn-orb"
              title="Configuración"
              aria-label="Configuración"
              active-class="!bg-ink !text-[var(--color-sobre-tinta)]"
            >
              <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.6" stroke="currentColor">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  d="M9.594 3.94c.09-.542.56-.94 1.11-.94h2.593c.55 0 1.02.398 1.11.94l.213 1.281c.063.374.313.686.645.87.074.04.147.083.22.127.324.196.72.257 1.075.124l1.217-.456a1.125 1.125 0 0 1 1.37.49l1.296 2.247a1.125 1.125 0 0 1-.26 1.431l-1.003.827c-.293.241-.438.613-.43.992a7.03 7.03 0 0 1 0 .255c-.008.378.137.75.43.991l1.004.827c.424.35.534.955.26 1.43l-1.298 2.247a1.125 1.125 0 0 1-1.369.491l-1.217-.456c-.355-.133-.75-.072-1.076.124a6.47 6.47 0 0 1-.22.128c-.331.183-.581.495-.644.869l-.213 1.281c-.09.542-.56.94-1.11.94h-2.594c-.55 0-1.019-.398-1.11-.94l-.213-1.281c-.062-.374-.312-.686-.644-.87a6.52 6.52 0 0 1-.22-.127c-.325-.196-.72-.257-1.076-.124l-1.217.456a1.125 1.125 0 0 1-1.369-.49l-1.297-2.247a1.125 1.125 0 0 1 .26-1.431l1.004-.827c.292-.24.437-.613.43-.991a6.93 6.93 0 0 1 0-.255c.007-.38-.138-.751-.43-.992l-1.004-.827a1.125 1.125 0 0 1-.26-1.43l1.297-2.247a1.125 1.125 0 0 1 1.37-.491l1.216.456c.356.133.751.072 1.077-.124.072-.044.146-.086.22-.128.331-.183.581-.495.644-.869l.213-1.28Z"
                />
                <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z" />
              </svg>
            </RouterLink>
            <BotonTema />
            <span class="avatar-ring h-10 w-10">
              <span class="text-sm font-semibold">{{ inicial }}</span>
            </span>
            <button type="button" class="btn-orb" title="Cerrar sesión" @click="confirmandoSalida = true">
              <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.6" stroke="currentColor">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  d="M15.75 9V5.25A2.25 2.25 0 0 0 13.5 3h-6a2.25 2.25 0 0 0-2.25 2.25v13.5A2.25 2.25 0 0 0 7.5 21h6a2.25 2.25 0 0 0 2.25-2.25V15M12 9l3 3m0 0-3 3m3-3H3"
                />
              </svg>
            </button>
          </div>
        </header>

        <main :class="anchoCompleto ? 'min-h-0 flex-1 pt-4' : 'py-7 sm:py-9'">
          <RouterView v-slot="{ Component, route }">
            <Transition name="page" mode="out-in">
              <component :is="Component" :key="route.path" />
            </Transition>
          </RouterView>
        </main>
    </div>

    <ConfirmDialog
      :abierto="confirmandoSalida"
      titulo="Cerrar sesión"
      mensaje="Vas a salir del sistema. Si tenés un informe en borrador sin guardar, vas a perder los cambios."
      texto-confirmar="Salir"
      texto-cancelar="Seguir trabajando"
      tono="peligro"
      @confirmar="salir"
      @cancelar="confirmandoSalida = false"
    />
  </div>
</template>
