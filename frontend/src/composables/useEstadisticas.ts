import { computed, type Ref } from 'vue'
import type { EstudioEstadistica, EstadoEstudio, PrioridadEstudio } from '@/types/estudio'
import type { Usuario } from '@/types/auth'

export type TonoKpi = 'neutro' | 'atencion' | 'alerta' | 'bien'

export interface Kpi {
  etiqueta: string
  valor: string
  detalle: string
  tono: TonoKpi
}

export interface Segmento {
  clave: string
  etiqueta: string
  valor: number
  color: string
}

// Cada barra trae su desglose: el largo total responde «cuánto» y los tramos
// «en qué punto del circuito está».
export interface BarraApilada {
  etiqueta: string
  total: number
  segmentos: Segmento[]
}

export interface PuntoTendencia {
  fecha: string
  etiqueta: string
  entradas: number
  salidas: number
}

export interface Serie {
  clave: string
  etiqueta: string
  color: string
}

export interface Panel {
  titulo: string
  subtitulo: string
}

const ESTADOS: { clave: EstadoEstudio; etiqueta: string; color: string }[] = [
  { clave: 'Pendiente', etiqueta: 'Pendiente', color: 'var(--color-viz-pendiente)' },
  { clave: 'EnInforme', etiqueta: 'En informe', color: 'var(--color-viz-informe)' },
  { clave: 'Informado', etiqueta: 'Informado', color: 'var(--color-viz-informado)' },
]

// De menor a mayor urgencia: el orden importa y la rampa de color lo acompaña,
// así se lee la gravedad sin volver a la leyenda.
const PRIORIDADES: { clave: PrioridadEstudio; etiqueta: string; color: string }[] = [
  { clave: 'Rutina', etiqueta: 'Rutina', color: 'var(--color-viz-rutina)' },
  { clave: 'Urgente', etiqueta: 'Urgente', color: 'var(--color-viz-urgente)' },
  { clave: 'Stat', etiqueta: 'STAT', color: 'var(--color-viz-stat)' },
]

// Más de ~7 categorías dejan de distinguirse; el resto se agrupa en «Otros».
const MAX_BARRAS = 6
const DIAS_TENDENCIA = 14
const MS_DIA = 86_400_000

// Descarta los pares incompletos o con fechas inválidas en lugar de propagar NaN:
// una API vieja que no manda todavía las marcas de tiempo tiene que dar «—», no «NaN d».
function duraciones(
  estudios: EstudioEstadistica[],
  desde: (e: EstudioEstadistica) => string | null,
  hasta: (e: EstudioEstadistica) => string | null,
): number[] {
  const minutos: number[] = []
  for (const estudio of estudios) {
    const inicio = desde(estudio)
    const fin = hasta(estudio)
    if (!inicio || !fin) continue
    const diferencia = (new Date(fin).getTime() - new Date(inicio).getTime()) / 60_000
    if (Number.isFinite(diferencia)) minutos.push(diferencia)
  }
  return minutos
}

// Mediana y no promedio: un solo estudio olvidado dos semanas corre la media y
// deja de describir el día típico.
function mediana(valores: number[]): number | null {
  if (valores.length === 0) return null
  const ordenados = [...valores].sort((a, b) => a - b)
  const medio = Math.floor(ordenados.length / 2)
  return ordenados.length % 2 === 0 ? (ordenados[medio - 1] + ordenados[medio]) / 2 : ordenados[medio]
}

function formatearDuracion(minutos: number | null): string {
  if (minutos === null) return '—'
  if (minutos < 60) return `${Math.round(minutos)} min`
  if (minutos < 60 * 48) return `${(minutos / 60).toFixed(1)} h`
  return `${(minutos / 1440).toFixed(1)} d`
}

function claveDeDia(fecha: Date): string {
  const mes = String(fecha.getMonth() + 1).padStart(2, '0')
  const dia = String(fecha.getDate()).padStart(2, '0')
  return `${fecha.getFullYear()}-${mes}-${dia}`
}

function apilarPor(
  estudios: EstudioEstadistica[],
  clave: (e: EstudioEstadistica) => string,
): BarraApilada[] {
  const grupos = new Map<string, EstudioEstadistica[]>()
  for (const estudio of estudios) {
    const k = clave(estudio) || '—'
    const grupo = grupos.get(k)
    if (grupo) grupo.push(estudio)
    else grupos.set(k, [estudio])
  }

  const construir = (etiqueta: string, items: EstudioEstadistica[]): BarraApilada => ({
    etiqueta,
    total: items.length,
    segmentos: ESTADOS.map(({ clave: estado, etiqueta: nombre, color }) => ({
      clave: estado,
      etiqueta: nombre,
      color,
      valor: items.filter((e) => e.estado === estado).length,
    })),
  })

  const ordenados = [...grupos.entries()].sort((a, b) => b[1].length - a[1].length)
  if (ordenados.length <= MAX_BARRAS) {
    return ordenados.map(([etiqueta, items]) => construir(etiqueta, items))
  }

  const visibles = ordenados.slice(0, MAX_BARRAS).map(([etiqueta, items]) => construir(etiqueta, items))
  const resto = ordenados.slice(MAX_BARRAS).flatMap(([, items]) => items)
  return [...visibles, construir('Otros', resto)]
}

function porPrioridad(estudios: EstudioEstadistica[]): Segmento[] {
  return PRIORIDADES.map(({ clave, etiqueta, color }) => ({
    clave,
    etiqueta,
    color,
    valor: estudios.filter((e) => e.prioridad === clave).length,
  }))
}

// Dos flujos sobre el mismo eje: lo que entra al circuito y lo que sale firmado.
function tendencia(entradas: (string | null)[], salidas: (string | null)[]): PuntoTendencia[] {
  const formato = new Intl.DateTimeFormat('es-AR', { day: '2-digit', month: '2-digit' })

  const hoy = new Date()
  hoy.setHours(0, 0, 0, 0)

  const contar = (fechas: (string | null)[]) => {
    const conteo = new Map<string, number>()
    for (const fecha of fechas) {
      if (!fecha) continue
      const k = claveDeDia(new Date(fecha))
      conteo.set(k, (conteo.get(k) ?? 0) + 1)
    }
    return conteo
  }

  const porDiaEntradas = contar(entradas)
  const porDiaSalidas = contar(salidas)

  return Array.from({ length: DIAS_TENDENCIA }, (_, i) => {
    const dia = new Date(hoy.getTime() - (DIAS_TENDENCIA - 1 - i) * MS_DIA)
    const k = claveDeDia(dia)
    return {
      fecha: k,
      etiqueta: formato.format(dia),
      entradas: porDiaEntradas.get(k) ?? 0,
      salidas: porDiaSalidas.get(k) ?? 0,
    }
  })
}

function kpi(etiqueta: string, valor: number | string, detalle: string, tono: TonoKpi = 'neutro'): Kpi {
  return { etiqueta, valor: typeof valor === 'number' ? String(valor) : valor, detalle, tono }
}

export function useEstadisticas(estudios: Ref<EstudioEstadistica[]>, usuario: Ref<Usuario | null>) {
  const rol = computed(() => usuario.value?.rol ?? 'Tecnico')
  const mios = computed(() => estudios.value.filter((e) => e.subidoPorId === usuario.value?.id))
  const asignadosAMi = computed(() => estudios.value.filter((e) => e.radiologoAsignadoId === usuario.value?.id))
  const pendientes = computed(() => estudios.value.filter((e) => e.estado === 'Pendiente'))

  // El universo que le toca a cada rol: sobre él se calculan gráficos y tiempos.
  const propios = computed(() => {
    if (rol.value === 'Tecnico') return mios.value
    if (rol.value === 'Radiologo') return asignadosAMi.value
    return estudios.value
  })

  function recientes(lista: EstudioEstadistica[], dias: number) {
    const desde = Date.now() - dias * MS_DIA
    return lista.filter((e) => new Date(e.createdAt).getTime() >= desde)
  }

  const kpis = computed<Kpi[]>(() => {
    if (rol.value === 'Radiologo') {
      const enCola = pendientes.value
      const urgentes = enCola.filter((e) => e.prioridad === 'Stat' || e.prioridad === 'Urgente').length
      const enInforme = asignadosAMi.value.filter((e) => e.estado === 'EnInforme').length
      const firmadosPorMi = asignadosAMi.value.filter((e) => !!e.informadoAt)
      const vencidosMios = asignadosAMi.value.filter((e) => e.vencido).length

      // De tomarlo a firmarlo: mi tramo del circuito, sin la espera en cola.
      const tiempoLectura = mediana(
        duraciones(
          firmadosPorMi,
          (e) => e.asignadoAt,
          (e) => e.informadoAt,
        ),
      )

      return [
        kpi('Pendientes en la cola', enCola.length, 'disponibles para tomar'),
        kpi('Alta prioridad en cola', urgentes, 'STAT o urgentes sin tomar', urgentes > 0 ? 'atencion' : 'neutro'),
        kpi('Míos en informe', enInforme, 'tomados, sin firmar'),
        kpi('Informados por mí', firmadosPorMi.length, 'con informe firmado', 'bien'),
        kpi('Míos fuera de plazo', vencidosMios, 'pasaron el SLA', vencidosMios > 0 ? 'alerta' : 'bien'),
        kpi('Mi tiempo de lectura', formatearDuracion(tiempoLectura), 'mediana de tomar a firmar'),
      ]
    }

    if (rol.value === 'Tecnico') {
      const esperando = mios.value.filter((e) => e.estado !== 'Informado').length
      const informados = mios.value.filter((e) => e.estado === 'Informado').length
      const vencidosMios = mios.value.filter((e) => e.vencido).length
      const demora = mediana(
        duraciones(
          mios.value,
          (e) => e.createdAt,
          (e) => e.informadoAt,
        ),
      )

      return [
        kpi('Estudios que subí', mios.value.length, 'en total'),
        kpi('Subidos últimos 7 días', recientes(mios.value, 7).length, 'tu ritmo reciente'),
        kpi('Esperando informe', esperando, 'todavía sin firmar'),
        kpi('Ya informados', informados, 'devueltos al hospital', 'bien'),
        kpi('Míos fuera de plazo', vencidosMios, 'pasaron el SLA', vencidosMios > 0 ? 'alerta' : 'bien'),
        kpi('Demora hasta el informe', formatearDuracion(demora), 'mediana de subida a firma'),
      ]
    }

    const firmados = estudios.value.filter((e) => !!e.informadoAt)
    const sinAsignar = pendientes.value.length
    const enInforme = estudios.value.filter((e) => e.estado === 'EnInforme').length
    const vencidos = estudios.value.filter((e) => e.vencido).length

    const medibles = firmados.filter((e) => !!e.fechaLimite)
    const cumplimiento =
      medibles.length === 0
        ? null
        : Math.round(
            (medibles.filter((e) => new Date(e.informadoAt!) <= new Date(e.fechaLimite)).length /
              medibles.length) *
              100,
          )

    // Del alta del estudio a la firma: el número que mide el hospital.
    const vueltaCompleta = mediana(
      duraciones(
        firmados,
        (e) => e.createdAt,
        (e) => e.informadoAt,
      ),
    )

    return [
      kpi('Estudios totales', estudios.value.length, 'en la plataforma'),
      kpi('Sin asignar', sinAsignar, 'esperando radiólogo', sinAsignar > 0 ? 'atencion' : 'neutro'),
      kpi('En informe', enInforme, 'tomados, sin firmar'),
      kpi('Informados', firmados.length, 'circuito cerrado', 'bien'),
      kpi('Fuera de plazo', vencidos, 'abiertos con SLA vencido', vencidos > 0 ? 'alerta' : 'bien'),
      kpi(
        'Cumplimiento de SLA',
        cumplimiento === null ? '—' : `${cumplimiento}%`,
        'informados dentro del plazo',
        cumplimiento === null ? 'neutro' : cumplimiento >= 90 ? 'bien' : cumplimiento >= 75 ? 'atencion' : 'alerta',
      ),
      kpi('Vuelta completa', formatearDuracion(vueltaCompleta), 'mediana de alta a firma'),
      kpi('Hospitales activos', new Set(estudios.value.map((e) => e.hospitalNombre)).size, 'derivando estudios'),
    ]
  })

  // El radiólogo mira toda la plataforma y no solo lo suyo: el tramo «Pendiente» de
  // cada modalidad es justamente lo que puede tomar.
  const barras = computed<BarraApilada[]>(() => {
    if (rol.value === 'Admin') return apilarPor(estudios.value, (e) => e.hospitalNombre)
    if (rol.value === 'Radiologo') return apilarPor(estudios.value, (e) => e.modalidad)
    return apilarPor(mios.value, (e) => e.modalidad)
  })

  const panelBarras = computed<Panel>(() => {
    if (rol.value === 'Radiologo') {
      return { titulo: 'Cola y carga por modalidad', subtitulo: 'Lo que espera para leer y lo que ya está en curso' }
    }
    if (rol.value === 'Tecnico') {
      return { titulo: 'Mis envíos por modalidad', subtitulo: 'Lo que subiste, y en qué punto quedó cada uno' }
    }
    return { titulo: 'Estudios por hospital', subtitulo: 'De dónde llega el volumen, y en qué estado está' }
  })

  const leyendaBarras = computed<Serie[]>(() =>
    ESTADOS.map(({ clave, etiqueta, color }) => ({ clave, etiqueta, color })),
  )

  const dona = computed<Segmento[]>(() =>
    porPrioridad(rol.value === 'Radiologo' ? pendientes.value : propios.value),
  )

  const panelDona = computed<Panel>(() => {
    if (rol.value === 'Radiologo') {
      return { titulo: 'Urgencia de la cola', subtitulo: 'Con qué prioridad entra lo que falta leer' }
    }
    if (rol.value === 'Tecnico') {
      return { titulo: 'Urgencia de mis envíos', subtitulo: 'Con qué prioridad los mandaste' }
    }
    return { titulo: 'Urgencia de la plataforma', subtitulo: 'Cómo se reparte la prioridad clínica' }
  })

  // El radiólogo no «recibe» estudios: entra a su circuito cuando lo toma.
  const puntosTendencia = computed<PuntoTendencia[]>(() =>
    rol.value === 'Radiologo'
      ? tendencia(
          asignadosAMi.value.map((e) => e.asignadoAt),
          asignadosAMi.value.map((e) => e.informadoAt),
        )
      : tendencia(
          propios.value.map((e) => e.createdAt),
          propios.value.map((e) => e.informadoAt),
        ),
  )

  const seriesTendencia = computed<Serie[]>(() => [
    {
      clave: 'entradas',
      etiqueta: rol.value === 'Radiologo' ? 'Tomados' : 'Recibidos',
      color: 'var(--color-viz-serie)',
    },
    { clave: 'salidas', etiqueta: 'Firmados', color: 'var(--color-viz-informado)' },
  ])

  const panelTendencia = computed<Panel>(() => {
    if (rol.value === 'Radiologo') {
      return { titulo: 'Mi ritmo de lectura', subtitulo: 'Estudios tomados y firmados, últimos 14 días' }
    }
    if (rol.value === 'Tecnico') {
      return { titulo: 'Mi ritmo de envío', subtitulo: 'Estudios subidos y devueltos firmados, últimos 14 días' }
    }
    return { titulo: 'Entradas y salidas', subtitulo: 'Estudios recibidos y firmados, últimos 14 días' }
  })

  // Si entra más de lo que sale la cola crece: es el dato que anticipa el atasco.
  const balanceTendencia = computed(() => {
    const puntos = puntosTendencia.value
    const entradas = puntos.reduce((s, p) => s + p.entradas, 0)
    const salidas = puntos.reduce((s, p) => s + p.salidas, 0)
    return { entradas, salidas, neto: entradas - salidas }
  })

  return {
    kpis,
    barras,
    panelBarras,
    leyendaBarras,
    dona,
    panelDona,
    puntosTendencia,
    seriesTendencia,
    panelTendencia,
    balanceTendencia,
  }
}
