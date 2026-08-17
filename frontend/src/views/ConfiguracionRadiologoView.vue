<script setup lang="ts">
import { useTemaStore } from '@/stores/tema'
import TarjetaConfiguracion from '@/components/TarjetaConfiguracion.vue'

const tema = useTemaStore()

const opcionesTema = [
  { valor: 'claro', etiqueta: 'Claro' },
  { valor: 'oscuro', etiqueta: 'Oscuro' },
  { valor: 'sistema', etiqueta: 'Sistema' },
] as const
</script>

<template>
  <div class="stagger space-y-8">
    <div class="glass flex flex-wrap items-center gap-4 p-6 sm:p-7">
      <span class="flex h-12 w-12 flex-none items-center justify-center rounded-2xl bg-[var(--color-superficie-suave)]">
        <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="1.6" stroke="currentColor">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M9.594 3.94c.09-.542.56-.94 1.11-.94h2.593c.55 0 1.02.398 1.11.94l.213 1.281c.063.374.313.686.645.87.074.04.147.083.22.127.324.196.72.257 1.075.124l1.217-.456a1.125 1.125 0 0 1 1.37.49l1.296 2.247a1.125 1.125 0 0 1-.26 1.431l-1.003.827c-.293.241-.438.613-.43.992a7.03 7.03 0 0 1 0 .255c-.008.378.137.75.43.991l1.004.827c.424.35.534.955.26 1.43l-1.298 2.247a1.125 1.125 0 0 1-1.369.491l-1.217-.456c-.355-.133-.75-.072-1.076.124a6.47 6.47 0 0 1-.22.128c-.331.183-.581.495-.644.869l-.213 1.281c-.09.542-.56.94-1.11.94h-2.594c-.55 0-1.019-.398-1.11-.94l-.213-1.281c-.062-.374-.312-.686-.644-.87a6.52 6.52 0 0 1-.22-.127c-.325-.196-.72-.257-1.076-.124l-1.217.456a1.125 1.125 0 0 1-1.369-.49l-1.297-2.247a1.125 1.125 0 0 1 .26-1.431l1.004-.827c.292-.24.437-.613.43-.991a6.93 6.93 0 0 1 0-.255c.007-.38-.138-.751-.43-.992l-1.004-.827a1.125 1.125 0 0 1-.26-1.43l1.297-2.247a1.125 1.125 0 0 1 1.37-.491l1.216.456c.356.133.751.072 1.077-.124.072-.044.146-.086.22-.128.331-.183.581-.495.644-.869l.213-1.28Z"
          />
          <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z" />
        </svg>
      </span>
      <div>
        <h1 class="display text-2xl sm:text-3xl">Configuración</h1>
        <p class="meta-label mt-1">Tu espacio de trabajo</p>
      </div>
    </div>

    <section class="space-y-4">
      <div class="flex items-center gap-4">
        <span class="h-4 w-1 flex-none rounded-full bg-gradient-to-b from-[var(--color-coral)] via-[var(--color-lilac)] to-[var(--color-aqua)]" />
        <p class="meta-label flex-none">Informes</p>
        <span class="h-px flex-1 bg-[var(--color-hairline)]" />
      </div>

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <TarjetaConfiguracion
          titulo="Plantillas de informe"
          descripcion="Estructuras que reusás al redactar. Aparecen como opción al empezar un informe, filtradas por la modalidad del estudio."
          destino="/plantillas"
        >
          <template #icono>
            <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="1.6" stroke="currentColor">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                d="M8.25 7.5V6.108c0-1.135.845-2.098 1.976-2.192.373-.03.748-.057 1.123-.08M15.75 18H18a2.25 2.25 0 0 0 2.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 0 0-1.123-.08M15.75 18.75v-1.875a3.375 3.375 0 0 0-3.375-3.375h-1.5a1.125 1.125 0 0 1-1.125-1.125v-1.5A3.375 3.375 0 0 0 6.375 7.5H5.25m11.9-3.664A2.251 2.251 0 0 0 15 2.25h-1.5a2.251 2.251 0 0 0-2.15 1.586m5.8 0c.065.21.1.433.1.664v.75h-6V4.5c0-.231.035-.454.1-.664M6.75 7.5H4.875c-.621 0-1.125.504-1.125 1.125v12c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V16.5a9 9 0 0 0-9-9Z"
              />
            </svg>
          </template>
        </TarjetaConfiguracion>
      </div>
    </section>

    <section class="space-y-4">
      <div class="flex items-center gap-4">
        <span class="h-4 w-1 flex-none rounded-full bg-gradient-to-b from-[var(--color-coral)] via-[var(--color-lilac)] to-[var(--color-aqua)]" />
        <p class="meta-label flex-none">Apariencia</p>
        <span class="h-px flex-1 bg-[var(--color-hairline)]" />
      </div>

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <TarjetaConfiguracion
          titulo="Tema de la interfaz"
          descripcion="El modo oscuro reduce el brillo alrededor de las imágenes: en salas a oscuras un marco claro falsea la percepción de los grises."
        >
          <template #icono>
            <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="1.6" stroke="currentColor">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                d="M21.752 15.002A9.72 9.72 0 0 1 18 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 0 0 3 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 0 0 9.002-5.998Z"
              />
            </svg>
          </template>

          <div class="mt-4 flex flex-wrap gap-2">
            <button
              v-for="o in opcionesTema"
              :key="o.valor"
              type="button"
              class="chip"
              :class="tema.preferencia === o.valor ? 'chip-informe' : 'chip-neutro'"
              @click="tema.establecer(o.valor)"
            >
              {{ o.etiqueta }}
            </button>
          </div>
        </TarjetaConfiguracion>
      </div>
    </section>
  </div>
</template>
