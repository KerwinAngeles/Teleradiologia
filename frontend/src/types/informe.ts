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
}
