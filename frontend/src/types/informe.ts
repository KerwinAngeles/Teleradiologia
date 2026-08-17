export type EstadoInforme = 'Borrador' | 'Firmado'

export interface Informe {
  id: string
  estudioId: string
  radiologoId: string
  radiologoNombre: string
  contenido: string
  estado: EstadoInforme
  esAdenda: boolean
  informeAnteriorId: string | null
  createdAt: string
  firmadoAt: string | null
  hashContenido: string | null
  algoritmoFirma: string | null
  firmanteNombre: string | null
  firmanteMatricula: string | null
  firmaImagen: string | null
}

// Fila del listado general de informes: sin contenido, que solo hace falta en la hoja.
export interface InformeListado {
  id: string
  estudioId: string
  pacienteNombre: string
  pacienteDocumento: string
  modalidad: string
  hospitalNombre: string
  fechaEstudio: string
  estado: EstadoInforme
  esAdenda: boolean
  createdAt: string
  firmadoAt: string | null
  radiologoNombre: string
}

export interface InformeDetalle extends InformeListado {
  descripcionEstudio: string | null
  contenido: string
  hashContenido: string | null
  algoritmoFirma: string | null
  firmanteNombre: string | null
  firmanteMatricula: string | null
  firmaImagen: string | null
}

export interface VerificacionFirma {
  informeId: string
  valida: boolean
  hashCoincide: boolean
  firmaValida: boolean
  motivo: string | null
  hashGuardado: string | null
  hashCalculado: string
  algoritmo: string | null
  firmanteNombre: string | null
  firmanteMatricula: string | null
  firmadoAt: string | null
}
