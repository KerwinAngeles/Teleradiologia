<script setup lang="ts">
import { ref } from 'vue'
import { isAxiosError } from 'axios'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import ScanOrb from '@/components/ScanOrb.vue'
import LogoMark from '@/components/LogoMark.vue'
import BotonTema from '@/components/BotonTema.vue'

const toasts = useToastStore()

const email = ref('')
const password = ref('')
const cargando = ref(false)
const error = ref<string | null>(null)

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

async function onSubmit() {
  error.value = null
  cargando.value = true
  try {
    await auth.login(email.value, password.value)
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    router.push(redirect)
    toasts.exito(`Bienvenido, ${auth.usuario?.nombreCompleto ?? ''}.`)
  } catch (e) {
    const mensaje = !isAxiosError(e)
      ? 'Ocurrió un error inesperado al iniciar sesión.'
      : (e.response?.data?.detail ??
        (e.response
          ? `El servidor respondió con un error (${e.response.status}). Revisá que el API esté corriendo.`
          : 'No se pudo conectar con el servidor. Verificá que el API esté levantado en el puerto 5080.'))
    error.value = mensaje
    toasts.error(mensaje)
  } finally {
    cargando.value = false
  }
}
</script>

<template>
  <div class="aurora min-h-screen">
    <div class="absolute top-5 right-5 z-10">
      <BotonTema />
    </div>

    <div class="grid min-h-screen grid-cols-1 lg:grid-cols-[1.05fr_0.95fr]">
        <section class="fluting relative hidden flex-col justify-between p-10 lg:flex xl:p-14">
          <div class="flex items-center gap-2.5">
            <LogoMark :size="30" />
            <span class="text-sm font-semibold tracking-[0.2em] uppercase">Teleradiología</span>
          </div>

          <div class="flex flex-1 items-center justify-center py-8">
            <ScanOrb :size="380" />
          </div>

          <div class="max-w-md">
            <h2 class="display text-2xl leading-tight xl:text-3xl">Diagnóstico a distancia,<br />sin perder el detalle</h2>
            <p class="text-ink-soft mt-3 text-sm leading-relaxed">
              El hospital sube el estudio DICOM, el radiólogo lo lee y lo informa desde donde esté. Cada acceso queda
              auditado.
            </p>
            <div class="text-ink-faint mt-6 flex flex-wrap gap-x-6 gap-y-2 text-xs tracking-[0.1em] uppercase">
              <span>Estudios DICOM</span>
              <span>Informes firmados</span>
              <span>Trazabilidad</span>
            </div>
          </div>
        </section>

        <section class="flex items-center justify-center p-6 sm:p-10">
          <div class="w-full max-w-md">
            <div class="mb-6 flex items-center gap-2.5 lg:hidden">
              <LogoMark :size="30" />
              <span class="text-sm font-semibold tracking-[0.2em] uppercase">Teleradiología</span>
            </div>

            <div class="glass-solid p-7 sm:p-9">
              <p class="meta-label">Acceso clínico</p>
              <h1 class="display mt-2 text-2xl">Iniciar sesión</h1>
              <p class="text-ink-soft mt-2 text-sm">
                Plataforma para radiólogos y técnicos autorizados.
              </p>

              <form class="mt-7 space-y-4" @submit.prevent="onSubmit">
                <div>
                  <label class="meta-label mb-1.5 block" for="email">Email</label>
                  <input
                    id="email"
                    v-model="email"
                    type="email"
                    required
                    autocomplete="username"
                    placeholder="nombre@hospital.org"
                    class="field"
                  />
                </div>
                <div>
                  <label class="meta-label mb-1.5 block" for="password">Contraseña</label>
                  <input
                    id="password"
                    v-model="password"
                    type="password"
                    required
                    autocomplete="current-password"
                    placeholder="••••••••"
                    class="field"
                  />
                </div>

                <p v-if="error" class="rounded-xl bg-red-500/10 px-3 py-2 text-sm text-red-700">{{ error }}</p>

                <button type="submit" :disabled="cargando" class="btn-ink w-full">
                  {{ cargando ? 'Verificando…' : 'Entrar' }}
                  <svg
                    v-if="!cargando"
                    class="h-4 w-4"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke-width="1.8"
                    stroke="currentColor"
                  >
                    <path stroke-linecap="round" stroke-linejoin="round" d="M13.5 4.5 21 12m0 0-7.5 7.5M21 12H3" />
                  </svg>
                </button>
              </form>

              <p class="text-ink-soft mt-6 text-center text-sm">
                ¿No tenés cuenta?
                <RouterLink to="/registro" class="font-medium underline underline-offset-4">Solicitar acceso</RouterLink>
              </p>
            </div>

            <p class="text-ink-faint mt-5 text-center text-xs">
              Datos de salud protegidos · cada visualización queda registrada
            </p>
          </div>
        </section>
    </div>
  </div>
</template>
