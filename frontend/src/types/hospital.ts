export interface Hospital {
  id: string
  nombre: string
  codigoExterno: number | null
  provincia: string | null
  municipio: string | null
  emailContacto: string | null
  activo: boolean
  slaStatMinutos: number | null
  slaUrgenteMinutos: number | null
  slaRutinaMinutos: number | null
}

export interface EstablecimientoCatalogo {
  codigo: number
  nombre: string
  nivelAtencion: string | null
  tipo: string | null
  provincia: string | null
  municipio: string | null
}
