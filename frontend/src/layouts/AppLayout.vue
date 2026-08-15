<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import LogoMark from '@/components/LogoMark.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'

const auth = useAuthStore()
const toasts = useToastStore()
const router = useRouter()

const confirmandoSalida = ref(false)

const rolLabel: Record<string, string> = {
  Tecnico: 'Técnico',
  Radiologo: 'Radiólogo',
  Admin: 'Admin',
}

const inicial = computed(() => auth.usuario?.nombreCompleto.charAt(0).toUpperCase() ?? '?')

function salir() {
  confirmandoSalida.value = false
  auth.logout()
  router.push({ name: 'login' })
  toasts.info('Cerraste sesión.')
}
</script>

<template>
  <div class="aurora min-h-screen">
    <div class="mx-auto max-w-[1400px] px-4 py-5 sm:px-8 sm:py-7">
        <header class="glass flex flex-wrap items-center justify-between gap-4 px-4 py-3 sm:px-5">
          <div class="flex items-center gap-6">
            <RouterLink to="/" class="flex items-center gap-2.5">
              <LogoMark />
              <span class="hidden text-sm font-semibold tracking-[0.18em] uppercase sm:block">Teleradiología</span>
            </RouterLink>

            <nav class="flex items-center gap-1">
              <RouterLink
                to="/"
                class="text-ink-soft hover:text-ink rounded-full px-3.5 py-2 text-sm font-medium transition-colors hover:bg-white/70"
                active-class="!bg-ink !text-white"
              >
                Worklist
              </RouterLink>
              <RouterLink
                v-if="auth.usuario?.rol === 'Tecnico' || auth.usuario?.rol === 'Admin'"
                to="/subir"
                class="text-ink-soft hover:text-ink rounded-full px-3.5 py-2 text-sm font-medium transition-colors hover:bg-white/70"
                active-class="!bg-ink !text-white"
              >
                Subir estudio
              </RouterLink>
              <RouterLink
                v-if="auth.usuario?.rol === 'Admin'"
                to="/usuarios"
                class="text-ink-soft hover:text-ink rounded-full px-3.5 py-2 text-sm font-medium transition-colors hover:bg-white/70"
                active-class="!bg-ink !text-white"
              >
                Usuarios
              </RouterLink>
            </nav>
          </div>

          <div class="flex items-center gap-3">
            <div class="hidden text-right sm:block">
              <p class="text-sm leading-tight font-medium">{{ auth.usuario?.nombreCompleto }}</p>
              <p class="meta-label leading-tight">{{ rolLabel[auth.usuario?.rol ?? ''] }}</p>
            </div>
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

        <main class="py-7 sm:py-9">
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
