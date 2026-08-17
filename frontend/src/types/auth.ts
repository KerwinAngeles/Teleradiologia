export type Rol = 'Tecnico' | 'Radiologo' | 'Admin'

export type EstadoAcceso = 'Pendiente' | 'Aprobado' | 'Rechazado' | 'Suspendido'

export interface Usuario {
  id: string
  nombreCompleto: string
  email: string
  rol: Rol
  estadoAcceso: EstadoAcceso
  createdAt: string
  fechaDecision: string | null
  motivoDecision: string | null
  matricula: string | null
  activo: boolean
}

export interface LoginResponse {
  token: string
  refreshToken: string | null
  expiresAt: string
  usuario: Usuario
}

export interface RegistroResponse {
  usuarioId: string
  email: string
  estadoAcceso: EstadoAcceso
  mensaje: string
}
