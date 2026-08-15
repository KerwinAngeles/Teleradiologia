<script setup lang="ts">
import { ref } from 'vue'
import { isAxiosError } from 'axios'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import ScanOrb from '@/components/ScanOrb.vue'
import LogoMark from '@/components/LogoMark.vue'

const auth = useAuthStore()
const toasts = useToastStore()

const nombreCompleto = ref('')
const email = ref('')
const password = ref('')
const cargando = ref(false)
const error = ref<string | null>(null)
const enviado = ref(false)
const mensajeExito = ref('')

async function onSubmit() {
  error.value = null
  cargando.value = true
  try {
    const respuesta = await auth.registrar(nombreCompleto.value, email.value, password.value)
    mensajeExito.value = respuesta.mensaje
    enviado.value = true
    toasts.exito('Solicitud enviada.')
  } catch (e) {
    const mensaje = !isAxiosError(e)
      ? 'Ocurrió un error inesperado al crear la cuenta.'
      : (e.response?.data?.detail ??
        (e.response
          ? `El servidor respondió con un error (${e.response.status}).`
          : 'No se pudo conectar con el servidor.'))
    error.value = mensaje
    toasts.error(mensaje)
  } finally {
    cargando.value = false
  }
}
</script>

<template>
  <div class="aurora min-h-screen">
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
          <h2 class="display text-2xl leading-tight xl:text-3xl">Solicitá tu acceso<br />al sistema</h2>
          <p class="text-ink-soft mt-3 text-sm leading-relaxed">
            Creá tu cuenta y un administrador te va a habilitar con el rol que corresponda. Vas a recibir un email
            cuando esté lista.
          </p>
        </div>
      </section>

      <section class="flex items-center justify-center p-6 sm:p-10">
        <div class="w-full max-w-md">
          <div class="mb-6 flex items-center gap-2.5 lg:hidden">
            <LogoMark :size="30" />
            <span class="text-sm font-semibold tracking-[0.2em] uppercase">Teleradiología</span>
          </div>

          <Transition name="fade-slide" mode="out-in">
            <div v-if="enviado" key="ok" class="glass-solid p-7 text-center sm:p-9">
              <div class="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-[var(--color-aqua)]/40">
                <svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke-width="1.8" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" d="m4.5 12.75 6 6 9-13.5" />
                </svg>
              </div>
              <h1 class="display mt-5 text-2xl">Solicitud enviada</h1>
              <p class="text-ink-soft mt-3 text-sm leading-relaxed">{{ mensajeExito }}</p>
              <RouterLink to="/login" class="btn-ink mt-7 w-full">Volver al inicio de sesión</RouterLink>
            </div>

            <div v-else key="form" class="glass-solid p-7 sm:p-9">
              <p class="meta-label">Nueva cuenta</p>
              <h1 class="display mt-2 text-2xl">Registrarse</h1>
              <p class="text-ink-soft mt-2 text-sm">
                Registrarse no da acceso: un administrador tiene que habilitar la cuenta y asignarle un rol.
              </p>

              <form class="mt-7 space-y-4" @submit.prevent="onSubmit">
                <div>
                  <label class="meta-label mb-1.5 block" for="nombre">Nombre completo</label>
                  <input
                    id="nombre"
                    v-model="nombreCompleto"
                    type="text"
                    required
                    maxlength="200"
                    autocomplete="name"
                    placeholder="Dra. Ana Pérez"
                    class="field"
                  />
                </div>
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
                    minlength="8"
                    autocomplete="new-password"
                    placeholder="Mínimo 8 caracteres"
                    class="field"
                  />
                </div>

                <p v-if="error" class="rounded-xl bg-red-500/10 px-3 py-2 text-sm text-red-700">{{ error }}</p>

                <button type="submit" :disabled="cargando" class="btn-ink w-full">
                  {{ cargando ? 'Enviando…' : 'Solicitar acceso' }}
                </button>
              </form>

              <p class="text-ink-soft mt-6 text-center text-sm">
                ¿Ya tenés cuenta?
                <RouterLink to="/login" class="font-medium underline underline-offset-4">Iniciar sesión</RouterLink>
              </p>
            </div>
          </Transition>
        </div>
      </section>
    </div>
  </div>
</template>
