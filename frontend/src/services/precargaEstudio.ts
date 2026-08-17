import { api } from '@/services/api'
import type { Estudio, ImagenEstudio } from '@/types/estudio'
import type { Informe } from '@/types/informe'

export interface EstudioPrecargado {
  estudio: Estudio
  imagenes: ImagenEstudio[]
  informes: Informe[]
}

// Se guarda la promesa y no el resultado, y el hueco se ocupa apenas arranca la
// petición. Así la vista siempre recibe la carga en curso —aunque el guard se haya
// cansado de esperarla— en vez de pedir los mismos datos por segunda vez, y no queda
// nunca un resultado viejo guardado esperando a que alguien lo consuma.
let pendiente: { id: string; promesa: Promise<EstudioPrecargado> } | null = null

/**
 * Baja todo lo que la pantalla del estudio necesita para dibujarse entera. Se llama
 * desde el guard de la ruta, con la pantalla de carga ya puesta, para que la
 * navegación se confirme recién cuando hay algo completo que mostrar.
 */
export function precargarEstudio(id: string): Promise<EstudioPrecargado> {
  const promesa = (async (): Promise<EstudioPrecargado> => {
    const [{ data: estudio }, { data: imagenes }, { data: informes }] = await Promise.all([
      api.get<Estudio>(`/estudios/${id}`),
      api.get<ImagenEstudio[]>(`/estudios/${id}/imagenes`),
      api.get<Informe[]>(`/estudios/${id}/informes`),
    ])

    return { estudio, imagenes, informes }
  })()

  // Sin este catch, un rechazo que nadie llegue a consumir sube como
  // unhandledrejection. Quien la espere de verdad recibe el error igual.
  promesa.catch(() => {})

  pendiente = { id, promesa }
  return promesa
}

/**
 * Se consume una sola vez. Devolver null es una respuesta válida —entrada por otro
 * camino, u otro estudio— y ahí la vista pide los datos ella misma.
 */
export function tomarPrecarga(id: string): Promise<EstudioPrecargado> | null {
  if (pendiente?.id !== id) return null

  const { promesa } = pendiente
  pendiente = null
  return promesa
}
