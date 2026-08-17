<script setup lang="ts">
import { ref, computed } from 'vue'
import InformePanel from '@/components/InformePanel.vue'
import type { Estudio, ImagenEstudio } from '@/types/estudio'
import type { Informe } from '@/types/informe'

const props = defineProps<{
  estudio: Estudio
  informes: Informe[]
  imagenes: ImagenEstudio[]
}>()

const emit = defineEmits<{ actualizar: [] }>()

type Pestana = 'informe' | 'estudio' | 'versiones'

const pestana = ref<Pestana>('informe')

const pestanas: { clave: Pestana; etiqueta: string }[] = [
  { clave: 'informe', etiqueta: 'Informe' },
  { clave: 'estudio', etiqueta: 'Estudio' },
  { clave: 'versiones', etiqueta: 'Versiones' },
]

const formatoFecha = new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
const formatoFechaHora = new Intl.DateTimeFormat('es-AR', { dateStyle: 'short', timeStyle: 'short' })

const prioridadLabel: Record<string, string> = {
  Stat: 'STAT',
  Urgente: 'Urgente',
  Rutina: 'Rutina',
}

const cuadrosTotales = computed(() =>
  props.imagenes.reduce((suma, imagen) => suma + (imagen.numeroDeCuadros > 1 ? imagen.numeroDeCuadros : 1), 0),
)

const metadatos = computed(() => [
  { clave: 'Modalidad', valor: props.estudio.modalidad },
  { clave: 'Descripción', valor: props.estudio.descripcionEstudio ?? '—' },
  { clave: 'Hospital de origen', valor: props.estudio.hospitalNombre },
  { clave: 'Fecha del estudio', valor: formatoFecha.format(new Date(props.estudio.fechaEstudio)) },
  { clave: 'Instancias', valor: String(props.imagenes.length) },
  { clave: 'Cuadros', valor: String(cuadrosTotales.value) },
  { clave: 'Prioridad', valor: prioridadLabel[props.estudio.prioridad] ?? props.estudio.prioridad },
  { clave: 'Plazo', valor: formatoFechaHora.format(new Date(props.estudio.fechaLimite)) },
  { clave: 'Radiólogo', valor: props.estudio.radiologoAsignadoNombre ?? 'Sin asignar' },
  { clave: 'Subido por', valor: props.estudio.subidoPorNombre },
])

// El historial viene por fecha: el primero es el informe original y el resto adendas.
const versiones = computed(() =>
  props.informes.map((informe, i) => ({
    id: informe.id,
    titulo: `v${i + 1} · ${informe.esAdenda ? 'Adenda' : 'Informe original'}`,
    estado: informe.estado === 'Firmado' ? 'Firmado' : 'Borrador',
    firmado: informe.estado === 'Firmado',
    autor: informe.firmanteNombre ?? informe.radiologoNombre,
    fecha: formatoFechaHora.format(new Date(informe.firmadoAt ?? informe.createdAt)),
    detalle: informe.firmadoAt ? 'Firmado digitalmente' : 'Borrador sin firmar',
  })),
)
</script>

<template>
  <section class="glass flex min-h-0 flex-col overflow-hidden">
    <div class="flex flex-none border-b border-[var(--color-hairline)] px-2">
      <button
        v-for="p in pestanas"
        :key="p.clave"
        type="button"
        class="panel-tab"
        :class="pestana === p.clave && 'panel-tab-activa'"
        :aria-current="pestana === p.clave ? 'page' : undefined"
        @click="pestana = p.clave"
      >
        {{ p.etiqueta }}
      </button>
    </div>

    <div class="min-h-0 flex-1 overflow-y-auto p-5">
      <InformePanel
        v-if="pestana === 'informe'"
        variante="panel"
        :estudio="estudio"
        :informes="informes"
        @actualizar="emit('actualizar')"
      />

      <div v-else-if="pestana === 'estudio'">
        <div v-for="dato in metadatos" :key="dato.clave" class="meta-row">
          <span class="meta-label">{{ dato.clave }}</span>
          <span class="text-right text-sm font-medium">{{ dato.valor }}</span>
        </div>
      </div>

      <div v-else>
        <p v-if="versiones.length === 0" class="text-ink-faint text-sm">
          Todavía no hay ninguna versión: el informe no se empezó a redactar.
        </p>

        <ol v-else class="space-y-0">
          <li v-for="(version, i) in versiones" :key="version.id" class="flex gap-3">
            <div class="flex flex-none flex-col items-center pt-1.5">
              <span
                class="h-2.5 w-2.5 rounded-full"
                :style="{
                  background: version.firmado
                    ? 'var(--color-estado-informado)'
                    : 'var(--color-estado-pendiente)',
                }"
              />
              <span
                v-if="i < versiones.length - 1"
                class="w-px flex-1 bg-[var(--color-hairline)]"
                aria-hidden="true"
              />
            </div>

            <div class="min-w-0 flex-1 pb-5">
              <div class="flex items-baseline gap-2">
                <p class="text-sm font-medium">{{ version.titulo }}</p>
                <span class="text-ink-faint ml-auto flex-none font-mono text-[0.6875rem]">{{ version.fecha }}</span>
              </div>
              <p class="text-ink-soft text-xs">{{ version.autor }}</p>
              <p
                class="mt-1 text-xs"
                :style="{
                  color: version.firmado ? 'var(--color-estado-informado)' : 'var(--color-estado-pendiente)',
                }"
              >
                {{ version.detalle }}
              </p>
            </div>
          </li>
        </ol>
      </div>
    </div>
  </section>
</template>
