<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { useNotificacionesStore } from '@/stores/notificaciones'
import type { Notificacion } from '@/types/notificacion'

const notificaciones = useNotificacionesStore()
const router = useRouter()

const abierto = ref(false)
const contenedor = ref<HTMLElement | null>(null)

function alternar() {
  abierto.value = !abierto.value
  if (abierto.value) notificaciones.cargarResumen()
}

function alClicAfuera(evento: MouseEvent) {
  if (abierto.value && contenedor.value && !contenedor.value.contains(evento.target as Node)) {
    abierto.value = false
  }
}

onMounted(() => document.addEventListener('click', alClicAfuera))
onBeforeUnmount(() => document.removeEventListener('click', alClicAfuera))

async function abrir(notificacion: Notificacion) {
  abierto.value = false
  await notificaciones.marcarLeida(notificacion.id)

  if (notificacion.estudioId) {
    router.push(`/estudios/${notificacion.estudioId}`)
  } else {
    router.push('/notificaciones')
  }
}

const formatoRelativo = new Intl.RelativeTimeFormat('es-AR', { numeric: 'auto' })

function haceCuanto(iso: string): string {
  const minutos = Math.round((Date.now() - new Date(iso).getTime()) / 60000)
  if (minutos < 1) return 'recién'
  if (minutos < 60) return formatoRelativo.format(-minutos, 'minute')
  if (minutos < 60 * 24) return formatoRelativo.format(-Math.round(minutos / 60), 'hour')
  return formatoRelativo.format(-Math.round(minutos / 1440), 'day')
}
</script>

<template>
  <div ref="contenedor" class="relative">
    <button
      type="button"
      class="btn-orb relative"
      :title="notificaciones.noLeidas > 0 ? `${notificaciones.noLeidas} sin leer` : 'Notificaciones'"
      aria-label="Notificaciones"
      @click="alternar"
    >
      <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.6" stroke="currentColor">
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          d="M14.857 17.082a23.848 23.848 0 0 0 5.454-1.31A8.967 8.967 0 0 1 18 9.75V9A6 6 0 0 0 6 9v.75a8.967 8.967 0 0 1-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 0 1-5.714 0m5.714 0a3 3 0 1 1-5.714 0"
        />
      </svg>

      <span
        v-if="notificaciones.noLeidas > 0"
        class="absolute -top-1 -right-1 flex h-[18px] min-w-[18px] items-center justify-center rounded-full bg-[var(--color-estado-pendiente)] px-1 text-[0.625rem] font-semibold text-white tabular-nums"
      >
        {{ notificaciones.noLeidas > 99 ? '99+' : notificaciones.noLeidas }}
      </span>

      <!-- Punto de conexión: si el tiempo real se cayó, conviene que se note. -->
      <span
        v-else-if="!notificaciones.conectado"
        class="absolute -right-0.5 -bottom-0.5 h-2 w-2 rounded-full bg-[var(--color-ink-faint)]"
        title="Sin conexión en vivo"
      />
    </button>

    <Transition name="fade-slide">
      <div
        v-if="abierto"
        class="glass-solid absolute right-0 z-30 mt-2 w-[22rem] overflow-hidden shadow-2xl"
      >
        <div class="flex items-center justify-between border-b border-[var(--color-hairline)] px-4 py-3">
          <p class="meta-label">Notificaciones</p>
          <button
            v-if="notificaciones.noLeidas > 0"
            type="button"
            class="text-ink-soft hover:text-ink text-xs underline underline-offset-4"
            @click="notificaciones.marcarTodasLeidas()"
          >
            Marcar todas leídas
          </button>
        </div>

        <div class="max-h-[26rem] overflow-y-auto">
          <button
            v-for="n in notificaciones.recientes"
            :key="n.id"
            type="button"
            class="flex w-full gap-3 border-b border-[var(--color-hairline)] px-4 py-3 text-left transition-colors last:border-0 hover:bg-[var(--color-superficie-suave)]"
            @click="abrir(n)"
          >
            <span
              class="mt-1.5 h-2 w-2 flex-none rounded-full"
              :class="n.leidaAt ? 'bg-transparent' : 'bg-[var(--color-estado-informe)]'"
            />
            <span class="min-w-0 flex-1">
              <span class="flex items-center gap-2">
                <span class="truncate text-sm font-medium">{{ n.titulo }}</span>
                <span v-if="n.prioridad === 'Stat'" class="chip chip-stat flex-none">STAT</span>
                <span v-else-if="n.prioridad === 'Urgente'" class="chip chip-urgente flex-none">Urgente</span>
              </span>
              <span class="text-ink-soft mt-0.5 block truncate text-xs">{{ n.mensaje }}</span>
              <span class="text-ink-faint mt-1 block text-[0.6875rem]">
                {{ haceCuanto(n.createdAt) }}<template v-if="n.hospitalNombre"> · {{ n.hospitalNombre }}</template>
              </span>
            </span>
          </button>

          <p v-if="notificaciones.recientes.length === 0" class="text-ink-faint px-4 py-10 text-center text-sm">
            No tenés notificaciones.
          </p>
        </div>

        <RouterLink
          to="/notificaciones"
          class="text-ink-soft hover:text-ink block border-t border-[var(--color-hairline)] px-4 py-3 text-center text-sm transition-colors"
          @click="abierto = false"
        >
          Ver todas
        </RouterLink>
      </div>
    </Transition>
  </div>
</template>
