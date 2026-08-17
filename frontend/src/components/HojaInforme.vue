<script setup lang="ts">
// La hoja del informe: mismo layout en pantalla y en papel. Vive en un componente
// propio porque la imprimen dos pantallas —el editor y la vista de lectura— y es el
// documento que lleva la firma: si las dos se dibujaran por separado, terminarían
// divergiendo y el PDF dejaría de coincidir con lo que se firmó.
defineProps<{
  hospitalNombre?: string
  pacienteNombre?: string
  pacienteDocumento?: string
  modalidad?: string
  fechaEstudio?: string
  firmadoAt?: string | null
  firmanteNombre?: string | null
  firmanteMatricula?: string | null
  firmaImagen?: string | null
}>()

const formatoFecha = new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
const formatoFechaHora = new Intl.DateTimeFormat('es-AR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})
</script>

<template>
  <article class="hoja">
    <header class="hoja-encabezado">
      <div class="flex items-start justify-between gap-6">
        <div>
          <p class="hoja-titulo">Informe radiológico</p>
          <p class="hoja-sub">{{ hospitalNombre }}</p>
        </div>
        <div class="text-right">
          <p class="hoja-sub">Emitido</p>
          <p class="text-sm tabular-nums">
            {{ formatoFecha.format(firmadoAt ? new Date(firmadoAt) : new Date()) }}
          </p>
        </div>
      </div>

      <dl class="hoja-datos">
        <div><dt>Paciente</dt><dd>{{ pacienteNombre }}</dd></div>
        <div><dt>Documento</dt><dd>{{ pacienteDocumento }}</dd></div>
        <div><dt>Modalidad</dt><dd>{{ modalidad }}</dd></div>
        <div>
          <dt>Fecha del estudio</dt>
          <dd>{{ fechaEstudio ? formatoFecha.format(new Date(fechaEstudio)) : '' }}</dd>
        </div>
      </dl>
    </header>

    <div class="hoja-cuerpo">
      <slot />
    </div>

    <!-- Pie con la firma: se repite en cada página al imprimir. -->
    <footer class="hoja-pie">
      <div class="hoja-firma">
        <img v-if="firmaImagen" :src="firmaImagen" alt="Firma del radiólogo" class="hoja-firma-trazo" />
        <div v-else class="hoja-firma-linea" />

        <p class="hoja-firma-nombre">{{ firmanteNombre }}</p>
        <p class="hoja-firma-datos">
          <template v-if="firmanteMatricula">Matrícula {{ firmanteMatricula }}</template>
          <template v-else>Médico radiólogo</template>
        </p>
        <p v-if="firmadoAt" class="hoja-firma-datos">
          Firmado digitalmente el {{ formatoFechaHora.format(new Date(firmadoAt)) }}
        </p>
        <p v-else class="hoja-firma-datos hoja-borrador">Borrador — sin firmar</p>
      </div>
    </footer>
  </article>
</template>
