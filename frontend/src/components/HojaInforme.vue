<script setup lang="ts">
import { computed } from 'vue'

// La hoja del informe: mismo layout en pantalla y en papel. Vive en un componente
// propio porque la imprimen dos pantallas —el editor y la vista de lectura— y es el
// documento que lleva la firma: si cada una lo dibujara por su cuenta, terminarían
// divergiendo y el PDF dejaría de coincidir con lo que se firmó.
//
// La cabecera y el pie van en el thead y el tfoot de una tabla de una sola celda:
// es lo único que los navegadores repiten al paginar, y en impresión se fijan
// dentro de la banda que esos espaciadores reservan.
const props = defineProps<{
  hospitalNombre?: string
  hospitalProvincia?: string | null
  hospitalMunicipio?: string | null
  pacienteNombre?: string
  pacienteDocumento?: string
  pacienteSexo?: string | null
  pacienteFechaNacimiento?: string | null
  modalidad?: string
  descripcionEstudio?: string | null
  fechaEstudio?: string
  identificadorEstudio?: string | null
  radiologoNombre?: string | null
  esAdenda?: boolean
  firmadoAt?: string | null
  firmanteNombre?: string | null
  firmanteMatricula?: string | null
  firmaImagen?: string | null
  codigoVerificacion?: string | null
  algoritmoFirma?: string | null
}>()

const formatoFecha = new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' })
const formatoFechaHora = new Intl.DateTimeFormat('es-AR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
})

// Dos letras de la institución, como la marca del diseño.
const iniciales = computed(() => {
  const palabras = (props.hospitalNombre ?? '').split(/\s+/).filter((p) => p.length > 2)
  return (
    palabras
      .slice(0, 2)
      .map((p) => p.charAt(0).toUpperCase())
      .join('') || '—'
  )
})

const estadoLabel = computed(() => {
  if (!props.firmadoAt) return 'Borrador'
  return props.esAdenda ? 'Adenda' : 'Informe final'
})

// Un DICOM sin fecha de nacimiento se guarda con el valor por defecto (año 1), que
// impreso queda como «31/12/1». Todo lo anterior a 1900 se trata como desconocido.
function fechaNacimientoValida(valor?: string | null): Date | null {
  if (!valor) return null
  const fecha = new Date(valor)
  if (Number.isNaN(fecha.getTime()) || fecha.getFullYear() < 1900) return null
  return fecha
}

// La edad se calcula al momento del estudio y no al de hoy: es la que corresponde
// al acto médico que se está informando.
const sexoEdad = computed(() => {
  const sexo = props.pacienteSexo ? props.pacienteSexo.charAt(0).toUpperCase() : null
  const nacimiento = fechaNacimientoValida(props.pacienteFechaNacimiento)

  if (!nacimiento) return sexo ?? '—'

  const referencia = props.fechaEstudio ? new Date(props.fechaEstudio) : new Date()
  let edad = referencia.getFullYear() - nacimiento.getFullYear()
  const mes = referencia.getMonth() - nacimiento.getMonth()
  if (mes < 0 || (mes === 0 && referencia.getDate() < nacimiento.getDate())) edad--

  if (edad < 0 || edad > 130) return sexo ?? '—'
  return sexo ? `${sexo} · ${edad} a` : `${edad} a`
})

const nacimiento = computed(() => {
  const fecha = fechaNacimientoValida(props.pacienteFechaNacimiento)
  return fecha ? formatoFecha.format(fecha) : '—'
})

const institucion = computed(
  () => [props.hospitalMunicipio, props.hospitalProvincia].filter(Boolean).join(', ') || '—',
)

// El identificador del estudio es largo: en la cabecera entra el tramo final, que
// es el que lo distingue.
const identificadorCorto = computed(() => {
  const id = props.identificadorEstudio
  if (!id) return null
  return id.length <= 24 ? id : `…${id.slice(-20)}`
})
</script>

<template>
  <article class="hoja">
    <table class="hoja-marco">
      <thead>
        <tr>
          <th>
            <div class="hoja-espacio-encabezado">
              <header class="hoja-encabezado">
                <div class="hoja-marca-bloque">
                  <div class="hoja-marca">{{ iniciales }}</div>
                  <div>
                    <div class="hoja-institucion">{{ hospitalNombre }}</div>
                    <div class="hoja-servicio">Servicio de Diagnóstico por Imágenes · Teleradiología</div>
                  </div>
                </div>
                <div class="hoja-derecha">
                  <div class="hoja-rotulo">{{ esAdenda ? 'Adenda' : 'Informe' }}</div>
                  <div class="hoja-codigo hoja-codigo-cabecera">{{ identificadorCorto ?? '—' }}</div>
                </div>
              </header>
            </div>
          </th>
        </tr>
      </thead>

      <tbody>
        <tr>
          <td>
            <div class="hoja-portada">
              <div>
                <div class="hoja-kicker">Documento clínico</div>
                <h1 class="hoja-titulo">
                  {{ esAdenda ? 'Adenda al informe' : 'Informe radiológico' }}
                </h1>
                <div v-if="descripcionEstudio" class="hoja-portada-sub">{{ descripcionEstudio }}</div>
              </div>
              <div class="hoja-portada-estado">
                <div class="hoja-estado">{{ estadoLabel }}</div>
                <div class="hoja-rotulo">Emitido</div>
                <div class="hoja-portada-fecha">
                  {{ formatoFechaHora.format(firmadoAt ? new Date(firmadoAt) : new Date()) }}
                </div>
              </div>
            </div>

            <dl class="hoja-datos">
              <div><dt>Paciente</dt><dd>{{ pacienteNombre }}</dd></div>
              <div><dt>Documento</dt><dd class="hoja-cifra">{{ pacienteDocumento }}</dd></div>
              <div><dt>Sexo / Edad</dt><dd>{{ sexoEdad }}</dd></div>
              <div><dt>F. nacimiento</dt><dd class="hoja-cifra">{{ nacimiento }}</dd></div>
              <div><dt>Modalidad</dt><dd>{{ modalidad }}</dd></div>
              <div>
                <dt>Fecha del estudio</dt>
                <dd class="hoja-cifra">{{ fechaEstudio ? formatoFecha.format(new Date(fechaEstudio)) : '—' }}</dd>
              </div>
              <div><dt>Institución remitente</dt><dd>{{ institucion }}</dd></div>
              <div><dt>Radiólogo</dt><dd>{{ firmanteNombre ?? radiologoNombre ?? '—' }}</dd></div>
            </dl>

            <div class="hoja-cuerpo">
              <slot />
            </div>

            <div class="hoja-nota">
              <div class="hoja-rotulo">Limitaciones del método</div>
              <p>
                Este informe se emite sobre las imágenes disponibles en el estudio referido y debe interpretarse en
                el contexto clínico del paciente. Ante discordancia entre la clínica y los hallazgos, se sugiere
                ampliar el estudio con el método que corresponda.
              </p>
            </div>

            <div class="hoja-validacion">
              <div class="hoja-verificacion">
                <div class="hoja-rotulo">Validación electrónica</div>
                <template v-if="firmadoAt && codigoVerificacion">
                  <div class="hoja-verificacion-texto">
                    Documento firmado digitalmente<template v-if="algoritmoFirma"> ({{ algoritmoFirma }})</template>.
                    Código de verificación:
                  </div>
                  <div class="hoja-codigo">{{ codigoVerificacion }}</div>
                </template>
                <div v-else class="hoja-borrador hoja-verificacion-texto">
                  Documento sin firmar. No válido para uso clínico ni legal.
                </div>
              </div>

              <div class="hoja-firma">
                <img v-if="firmaImagen" :src="firmaImagen" alt="Firma del radiólogo" class="hoja-firma-trazo" />
                <div v-else class="hoja-firma-linea" />

                <div class="hoja-firma-datos">
                  <div class="hoja-firma-nombre">{{ firmanteNombre ?? radiologoNombre ?? '—' }}</div>
                  <div class="hoja-firma-detalle">Médico/a especialista en Diagnóstico por Imágenes</div>
                  <div v-if="firmanteMatricula" class="hoja-firma-detalle hoja-cifra">
                    Matrícula {{ firmanteMatricula }}
                  </div>
                  <div v-if="firmadoAt" class="hoja-firma-sello">
                    Firmado digitalmente el {{ formatoFechaHora.format(new Date(firmadoAt)) }}
                  </div>
                  <div v-else class="hoja-firma-sello hoja-borrador">Borrador — sin firmar</div>
                </div>
              </div>
            </div>
          </td>
        </tr>
      </tbody>

      <tfoot>
        <tr>
          <td>
            <div class="hoja-espacio-pie">
              <footer class="hoja-pie">
                <div class="hoja-pie-texto">
                  {{ hospitalNombre }}<template v-if="institucion !== '—'"> · {{ institucion }}</template><br />
                  Documento clínico confidencial. Su reproducción parcial o uso no autorizado está prohibido.
                </div>
                <div class="hoja-cifra">{{ pacienteDocumento }}</div>
              </footer>
            </div>
          </td>
        </tr>
      </tfoot>
    </table>
  </article>
</template>
