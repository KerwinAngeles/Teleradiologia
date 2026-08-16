import type { PrioridadEstudio } from '@/types/estudio'

export type TipoNotificacion = 'EstudioNuevo' | 'EstudioUrgente' | 'InformeFirmado' | 'SlaPorVencer'

export interface Notificacion {
  id: string
  tipo: TipoNotificacion
  titulo: string
  mensaje: string
  estudioId: string | null
  pacienteNombre: string | null
  modalidad: string | null
  hospitalNombre: string | null
  prioridad: PrioridadEstudio | null
  leidaAt: string | null
  createdAt: string
  leida: boolean
}

export interface ResumenNotificaciones {
  noLeidas: number
  recientes: Notificacion[]
}
