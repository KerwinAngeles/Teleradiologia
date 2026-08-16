export type TipoOperacion = 'Creacion' | 'Modificacion' | 'Eliminacion'

export interface Evento {
  id: string
  entidad: string
  entidadId: string
  operacion: TipoOperacion
  usuarioId: string | null
  usuarioEmail: string | null
  cambios: string | null
  timestamp: string
}

export interface ConteoPorClave {
  clave: string
  cantidad: number
}

export interface KpisEventos {
  desde: string
  hasta: string
  total: number
  creaciones: number
  modificaciones: number
  eliminaciones: number
  usuariosActivos: number
  porEntidad: ConteoPorClave[]
  porUsuario: ConteoPorClave[]
}

// El backend serializa { campo: { antes, despues } } para una modificación,
// y { campo: valor } para un alta o una baja.
export type DetalleCambio = Record<string, { antes?: unknown; despues?: unknown } | unknown>
