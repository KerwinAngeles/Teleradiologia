<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, watch } from 'vue'
import { api } from '@/services/api'
import type { ImagenEstudio } from '@/types/estudio'

const props = defineProps<{
  estudioId: string
  imagenes: ImagenEstudio[]
  instanciaActiva: number
  presets: { nombre: string; ancho: number; centro: number }[]
}>()

const emit = defineEmits<{
  seleccionar: [indice: number]
  preset: [ancho: number, centro: number]
}>()

// Bajar cientos de PNG para un rail de 88px no compensa: pasado el tope las
// instancias restantes quedan como botones numerados, que navegan igual.
const MAX_MINIATURAS = 24

const miniaturas = ref<(string | null)[]>([])
const cargando = ref(true)

const conMiniatura = computed(() => props.imagenes.slice(0, MAX_MINIATURAS))
const resto = computed(() => props.imagenes.slice(MAX_MINIATURAS))

function liberar() {
  for (const url of miniaturas.value) {
    if (url) URL.revokeObjectURL(url)
  }
  miniaturas.value = []
}

async function cargarMiniaturas() {
  liberar()
  cargando.value = true
  miniaturas.value = conMiniatura.value.map(() => null)

  // En serie y no en paralelo: una ráfaga de 24 peticiones compite con la
  // descarga del DICOM que el visor está bajando para mostrar la imagen grande.
  for (const [i, imagen] of conMiniatura.value.entries()) {
    try {
      const { data } = await api.get<Blob>(
        `/estudios/${props.estudioId}/imagenes/${imagen.orthancInstanceId}`,
        { responseType: 'blob' },
      )
      miniaturas.value[i] = URL.createObjectURL(data)
    } catch {
      miniaturas.value[i] = null
    }
  }

  cargando.value = false
}

onMounted(cargarMiniaturas)
onBeforeUnmount(liberar)

watch(() => props.imagenes, cargarMiniaturas)
</script>

<template>
  <div class="flex w-[88px] flex-none flex-col gap-2 overflow-y-auto">
    <p class="visor-kicker flex-none">Series</p>

    <div class="flex flex-col gap-1.5">
      <button
        v-for="(imagen, i) in conMiniatura"
        :key="imagen.orthancInstanceId"
        type="button"
        class="relative block overflow-hidden rounded-lg border transition-colors"
        :class="
          i === instanciaActiva
            ? 'border-[var(--color-lilac)]'
            : 'border-white/10 hover:border-white/30'
        "
        :title="`Instancia ${imagen.numeroInstancia}`"
        @click="emit('seleccionar', i)"
      >
        <img
          v-if="miniaturas[i]"
          :src="miniaturas[i]!"
          :alt="`Vista previa de la instancia ${imagen.numeroInstancia}`"
          class="h-[58px] w-full bg-[#0b0b11] object-cover"
        />
        <span v-else class="flex h-[58px] w-full items-center justify-center bg-[#0b0b11]">
          <span class="visor-lectura">{{ cargando ? '···' : '—' }}</span>
        </span>

        <span
          class="visor-lectura absolute inset-x-0 bottom-0 bg-black/60 px-1 py-0.5 text-left !text-[0.5625rem] !text-white/75"
        >
          {{ imagen.numeroInstancia }}<template v-if="imagen.numeroDeCuadros > 1"> · {{ imagen.numeroDeCuadros }}c</template>
        </span>
      </button>

      <button
        v-for="(imagen, i) in resto"
        :key="imagen.orthancInstanceId"
        type="button"
        class="visor-lectura rounded-md border py-1 text-center transition-colors"
        :class="
          i + MAX_MINIATURAS === instanciaActiva
            ? 'border-[var(--color-lilac)] !text-white'
            : 'border-white/10 hover:border-white/30'
        "
        @click="emit('seleccionar', i + MAX_MINIATURAS)"
      >
        {{ imagen.numeroInstancia }}
      </button>
    </div>

    <template v-if="presets.length > 0">
      <p class="visor-kicker mt-2 flex-none">Presets</p>
      <div class="flex flex-col gap-1">
        <button
          v-for="preset in presets"
          :key="preset.nombre"
          type="button"
          class="visor-tool !justify-start !rounded-md !px-2 !py-1 !text-[0.6875rem]"
          @click="emit('preset', preset.ancho, preset.centro)"
        >
          {{ preset.nombre }}
        </button>
      </div>
    </template>
  </div>
</template>
