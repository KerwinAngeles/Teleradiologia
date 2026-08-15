export type EstadoEstudio = 'Pendiente' | 'EnInforme' | 'Informado'

export interface Estudio {
  id: string
  pacienteNombre: string
  pacienteDocumento: string
  modalidad: string
  descripcionEstudio: string | null
  hospitalOrigen: string
  fechaEstudio: string
  estado: EstadoEstudio
  radiologoAsignadoId: string | null
  radiologoAsignadoNombre: string | null
  subidoPorId: string
  subidoPorNombre: string
  createdAt: string
}

export interface ImagenEstudio {
  orthancInstanceId: string
  numeroInstancia: number
}
