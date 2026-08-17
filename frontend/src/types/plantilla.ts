export interface SeccionPlantilla {
  titulo: string
  contenido: string | null
  orden: number
}

export interface Plantilla {
  id: string
  nombre: string
  modalidad: string | null
  regionAnatomica: string | null
  descripcion: string | null
  secciones: SeccionPlantilla[]
  favorita: boolean
  vecesUsada: number
  createdAt: string
}

export interface GuardarPlantilla {
  nombre: string
  modalidad: string | null
  regionAnatomica: string | null
  descripcion: string | null
  secciones: { titulo: string; contenido: string | null; orden: number }[]
  favorita: boolean
}

// Las cinco secciones del informe radiológico estándar: es con lo que arranca una plantilla nueva.
export const SECCIONES_ESTANDAR: { titulo: string; contenido: string }[] = [
  { titulo: 'Datos del estudio', contenido: '' },
  { titulo: 'Técnica', contenido: '' },
  { titulo: 'Hallazgos', contenido: '' },
  { titulo: 'Impresión diagnóstica', contenido: '' },
  { titulo: 'Recomendaciones', contenido: '' },
]

export const MODALIDADES = [
  { valor: '', etiqueta: 'Cualquier modalidad' },
  { valor: 'CR', etiqueta: 'CR — Radiografía' },
  { valor: 'DX', etiqueta: 'DX — Radiografía digital' },
  { valor: 'CT', etiqueta: 'CT — Tomografía' },
  { valor: 'MR', etiqueta: 'MR — Resonancia' },
  { valor: 'US', etiqueta: 'US — Ecografía' },
  { valor: 'MG', etiqueta: 'MG — Mamografía' },
  { valor: 'RF', etiqueta: 'RF — Fluoroscopía' },
  { valor: 'NM', etiqueta: 'NM — Medicina nuclear' },
  { valor: 'PT', etiqueta: 'PT — PET' },
]
