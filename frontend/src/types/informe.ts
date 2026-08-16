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
