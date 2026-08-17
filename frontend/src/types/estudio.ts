export type EstadoEstudio = 'Pendiente' | 'EnInforme' | 'Informado'

export type PrioridadEstudio = 'Rutina' | 'Urgente' | 'Stat'

export type EstadoSla = 'EnPlazo' | 'PorVencer' | 'Vencido' | 'Cumplido' | 'Incumplido'

export interface Estudio {
  id: string
  pacienteNombre: string
  pacienteDocumento: string
  modalidad: string
  descripcionEstudio: string | null
  hospitalId: string
  hospitalNombre: string
  fechaEstudio: string
  estado: EstadoEstudio
  prioridad: PrioridadEstudio
  fechaLimite: string
  estadoSla: EstadoSla
  minutosRestantes: number
  asignadoAt: string | null
  informadoAt: string | null
  radiologoAsignadoId: string | null
  radiologoAsignadoNombre: string | null
  subidoPorId: string
  subidoPorNombre: string
  createdAt: string
}

// Proyección liviana para KPIs y gráficos: van sobre el total, no sobre la página visible.
export interface EstudioEstadistica {
  estado: EstadoEstudio
  prioridad: PrioridadEstudio
  modalidad: string
  hospitalNombre: string
  subidoPorId: string
  radiologoAsignadoId: string | null
  vencido: boolean
  createdAt: string
  asignadoAt: string | null
  informadoAt: string | null
  fechaLimite: string
}

export interface ImagenEstudio {
  orthancInstanceId: string
  numeroInstancia: number
  // 1 en las imágenes fijas; >1 en los cine de ecografía y fluoroscopía.
  numeroDeCuadros: number
}
