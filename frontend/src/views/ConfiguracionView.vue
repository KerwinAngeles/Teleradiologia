<script setup lang="ts">
import { ref, computed } from 'vue'
import { useTemaStore } from '@/stores/tema'
import TarjetaConfiguracion from '@/components/TarjetaConfiguracion.vue'

const tema = useTemaStore()

const busqueda = ref('')

interface Opcion {
  titulo: string
  descripcion: string
  destino?: string
  icono: 'hospital' | 'usuarios' | 'resumen' | 'eventos' | 'tema'
}

interface Seccion {
  nombre: string
  opciones: Opcion[]
}

const secciones: Seccion[] = [
  {
    nombre: 'Hospitales y acceso',
    opciones: [
      {
        titulo: 'Catálogo de hospitales',
        descripcion:
          'Centros que envían estudios. Se dan de alta desde el listado oficial del Ministerio de Salud o a mano.',
        destino: '/configuracion/hospitales',
        icono: 'hospital',
      },
      {
        titulo: 'Usuarios y solicitudes',
        descripcion:
          'Aprobá o rechazá quién entra a la plataforma y con qué rol. Suspendé accesos cuando haga falta.',
        destino: '/usuarios',
        icono: 'usuarios',
      },
    ],
  },
  {
    nombre: 'Operación',
    opciones: [
      {
        titulo: 'Resumen de actividad',
        descripcion:
          'Estudios recibidos, informes firmados y estado de la cola. El envío automático por email corre todos los días.',
        destino: '/configuracion/resumen',
        icono: 'resumen',
      },
      {
        titulo: 'Eventos',
        descripcion:
          'Bitácora de cambios: qué se creó, modificó o eliminó, quién lo hizo y cuándo, con el detalle campo por campo.',
        destino: '/configuracion/eventos',
        icono: 'eventos',
      },
    ],
  },
  {
    nombre: 'Apariencia',
    opciones: [
      {
        titulo: 'Tema de la interfaz',
        descripcion:
          'El modo oscuro reduce el brillo alrededor de las imágenes: en salas a oscuras un marco claro falsea la percepción de los grises.',
        icono: 'tema',
      },
    ],
  },
]

const filtradas = computed(() => {
  const texto = busqueda.value.trim().toLowerCase()
  if (!texto) return secciones

  return secciones
    .map((seccion) => ({
      ...seccion,
      opciones: seccion.opciones.filter(
        (o) =>
          o.titulo.toLowerCase().includes(texto) ||
          o.descripcion.toLowerCase().includes(texto) ||
          seccion.nombre.toLowerCase().includes(texto),
      ),
    }))
    .filter((seccion) => seccion.opciones.length > 0)
})

const sinResultados = computed(() => filtradas.value.length === 0)

const opcionesTema = [
  { valor: 'claro', etiqueta: 'Claro' },
  { valor: 'oscuro', etiqueta: 'Oscuro' },
  { valor: 'sistema', etiqueta: 'Sistema' },
] as const
</script>

<template>
  <div class="stagger space-y-8">
    <div class="glass flex flex-wrap items-center justify-between gap-5 p-6 sm:p-7">
      <div class="flex items-center gap-4">
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
          <h1 class="display text-2xl sm:text-3xl">Panel de configuración</h1>
          <p class="meta-label mt-1">Administración y parámetros del sistema</p>
        </div>
      </div>

      <div class="relative w-full sm:w-80">
        <svg
          class="text-ink-faint pointer-events-none absolute top-1/2 left-3.5 h-4 w-4 -translate-y-1/2"
          fill="none"
          viewBox="0 0 24 24"
          stroke-width="1.6"
          stroke="currentColor"
        >
          <path stroke-linecap="round" stroke-linejoin="round" d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z" />
        </svg>
        <input v-model="busqueda" type="search" placeholder="Buscar configuración…" class="field !pl-10" />
      </div>
    </div>

    <section v-for="seccion in filtradas" :key="seccion.nombre" class="space-y-4">
      <div class="flex items-center gap-4">
        <span class="h-4 w-1 flex-none rounded-full bg-gradient-to-b from-[var(--color-coral)] via-[var(--color-lilac)] to-[var(--color-aqua)]" />
        <p class="meta-label flex-none">{{ seccion.nombre }}</p>
        <span class="h-px flex-1 bg-[var(--color-hairline)]" />
        <span class="text-ink-faint flex-none text-xs tabular-nums">{{ seccion.opciones.length }}</span>
      </div>

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <TarjetaConfiguracion
          v-for="opcion in seccion.opciones"
          :key="opcion.titulo"
          :titulo="opcion.titulo"
          :descripcion="opcion.descripcion"
          :destino="opcion.destino"
        >
          <template #icono>
            <svg
              v-if="opcion.icono === 'hospital'"
              class="h-5 w-5"
              fill="none"
              viewBox="0 0 24 24"
              stroke-width="1.6"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                d="M3.75 21h16.5M4.5 3h15M5.25 3v18m13.5-18v18M9 6.75h1.5m-1.5 3h1.5m-1.5 3h1.5m3-6H15m-1.5 3H15m-1.5 3H15M9 21v-3.375c0-.621.504-1.125 1.125-1.125h3.75c.621 0 1.125.504 1.125 1.125V21"
              />
            </svg>
            <svg
              v-else-if="opcion.icono === 'usuarios'"
              class="h-5 w-5"
              fill="none"
              viewBox="0 0 24 24"
              stroke-width="1.6"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                d="M15 19.128a9.38 9.38 0 0 0 2.625.372 9.337 9.337 0 0 0 4.121-.952 4.125 4.125 0 0 0-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 0 1 8.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0 1 11.964-3.07M12 6.375a3.375 3.375 0 1 1-6.75 0 3.375 3.375 0 0 1 6.75 0Zm8.25 2.25a2.625 2.625 0 1 1-5.25 0 2.625 2.625 0 0 1 5.25 0Z"
              />
            </svg>
            <svg
              v-else-if="opcion.icono === 'resumen'"
              class="h-5 w-5"
              fill="none"
              viewBox="0 0 24 24"
              stroke-width="1.6"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 0 1 3 19.875v-6.75ZM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V8.625ZM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V4.125Z"
              />
            </svg>
            <svg
              v-else-if="opcion.icono === 'eventos'"
              class="h-5 w-5"
              fill="none"
              viewBox="0 0 24 24"
              stroke-width="1.6"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                d="M12 6v6h4.5m4.5 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"
              />
            </svg>
            <svg v-else class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="1.6" stroke="currentColor">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                d="M21.752 15.002A9.72 9.72 0 0 1 18 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 0 0 3 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 0 0 9.002-5.998Z"
              />
            </svg>
          </template>

          <div v-if="opcion.icono === 'tema'" class="mt-4 flex flex-wrap gap-2">
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

    <p v-if="sinResultados" class="text-ink-faint py-12 text-center text-sm">
      No hay opciones que coincidan con «{{ busqueda }}».
    </p>
  </div>
</template>
