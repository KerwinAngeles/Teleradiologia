import { computed, type Ref } from 'vue'
import type { EstudioEstadistica, EstadoEstudio } from '@/types/estudio'
import type { Usuario } from '@/types/auth'

export interface Kpi {
  etiqueta: string
  valor: number
  detalle: string
}

export interface Segmento {
  etiqueta: string
  valor: number
  color: string
}

export interface Barra {
  etiqueta: string
  valor: number
}

export interface Panel {
  titulo: string
  subtitulo: string
}

const ESTADOS: { clave: EstadoEstudio; etiqueta: string; color: string }[] = [
  { clave: 'Pendiente', etiqueta: 'Pendiente', color: 'var(--color-estado-pendiente)' },
  { clave: 'EnInforme', etiqueta: 'En informe', color: 'var(--color-estado-informe)' },
  { clave: 'Informado', etiqueta: 'Informado', color: 'var(--color-estado-informado)' },
]

// Más de ~7 categorías con color propio dejan de distinguirse.
const MAX_BARRAS = 6

function contarPor(estudios: EstudioEstadistica[], clave: (e: EstudioEstadistica) => string): Barra[] {
  const conteo = new Map<string, number>()
  for (const estudio of estudios) {
    const k = clave(estudio) || '—'
    conteo.set(k, (conteo.get(k) ?? 0) + 1)
  }

  const ordenadas = [...conteo.entries()]
    .map(([etiqueta, valor]) => ({ etiqueta, valor }))
    .sort((a, b) => b.valor - a.valor)

  if (ordenadas.length <= MAX_BARRAS) return ordenadas

  const visibles = ordenadas.slice(0, MAX_BARRAS)
  const resto = ordenadas.slice(MAX_BARRAS).reduce((suma, b) => suma + b.valor, 0)
  return [...visibles, { etiqueta: 'Otros', valor: resto }]
}

function porEstado(estudios: EstudioEstadistica[]): Segmento[] {
  return ESTADOS.map(({ clave, etiqueta, color }) => ({
    etiqueta,
    color,
    valor: estudios.filter((e) => e.estado === clave).length,
  }))
}

export function useEstadisticas(estudios: Ref<EstudioEstadistica[]>, usuario: Ref<Usuario | null>) {
  const rol = computed(() => usuario.value?.rol ?? 'Tecnico')
  const mios = computed(() => estudios.value.filter((e) => e.subidoPorId === usuario.value?.id))
  const asignadosAMi = computed(() => estudios.value.filter((e) => e.radiologoAsignadoId === usuario.value?.id))
  const pendientes = computed(() => estudios.value.filter((e) => e.estado === 'Pendiente'))

  const kpis = computed<Kpi[]>(() => {
    if (rol.value === 'Radiologo') {
      return [
        { etiqueta: 'Pendientes en la cola', valor: pendientes.value.length, detalle: 'disponibles para tomar' },
        {
          etiqueta: 'Míos en informe',
          valor: asignadosAMi.value.filter((e) => e.estado === 'EnInforme').length,
          detalle: 'tomados, sin firmar',
        },
        {
          etiqueta: 'Informados por mí',
          valor: asignadosAMi.value.filter((e) => e.estado === 'Informado').length,
          detalle: 'con informe firmado',
        },
      ]
    }

    if (rol.value === 'Tecnico') {
      return [
        { etiqueta: 'Estudios que subí', valor: mios.value.length, detalle: 'en total' },
        {
          etiqueta: 'Esperando informe',
          valor: mios.value.filter((e) => e.estado !== 'Informado').length,
          detalle: 'todavía sin firmar',
        },
        {
          etiqueta: 'Ya informados',
          valor: mios.value.filter((e) => e.estado === 'Informado').length,
          detalle: 'devueltos al hospital',
        },
      ]
    }

    return [
      { etiqueta: 'Estudios totales', valor: estudios.value.length, detalle: 'en la plataforma' },
      { etiqueta: 'Pendientes', valor: pendientes.value.length, detalle: 'sin radiólogo asignado' },
      {
        etiqueta: 'Informados',
        valor: estudios.value.filter((e) => e.estado === 'Informado').length,
        detalle: 'circuito cerrado',
      },
      {
        etiqueta: 'Hospitales',
        valor: new Set(estudios.value.map((e) => e.hospitalNombre)).size,
        detalle: 'derivando estudios',
      },
    ]
  })

  const dona = computed<Segmento[]>(() =>
    rol.value === 'Tecnico' ? porEstado(mios.value) : porEstado(estudios.value),
  )

  const panelDona = computed<Panel>(() => {
    if (rol.value === 'Tecnico') {
      return { titulo: 'Estado de mis envíos', subtitulo: 'Qué pasó con los estudios que subiste' }
    }
    if (rol.value === 'Radiologo') {
      return { titulo: 'Estado de la cola', subtitulo: 'Todos los estudios de la plataforma' }
    }
    return { titulo: 'Distribución por estado', subtitulo: 'Toda la plataforma' }
  })

  const barras = computed<Barra[]>(() => {
    if (rol.value === 'Radiologo') return contarPor(pendientes.value, (e) => e.modalidad)
    if (rol.value === 'Tecnico') return contarPor(mios.value, (e) => e.modalidad)
    return contarPor(estudios.value, (e) => e.hospitalNombre)
  })

  const panelBarras = computed<Panel>(() => {
    if (rol.value === 'Radiologo') {
      return { titulo: 'Cola pendiente por modalidad', subtitulo: 'Qué te espera para leer' }
    }
    if (rol.value === 'Tecnico') {
      return { titulo: 'Mis estudios por modalidad', subtitulo: 'Lo que subiste, por tipo' }
    }
    return { titulo: 'Estudios por hospital', subtitulo: 'De dónde llega el volumen' }
  })

  return { kpis, dona, panelDona, barras, panelBarras }
}
