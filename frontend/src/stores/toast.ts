import { ref } from 'vue'
import { defineStore } from 'pinia'

export type TipoToast = 'exito' | 'error' | 'info' | 'cargando'

export interface Toast {
  id: number
  tipo: TipoToast
  mensaje: string
}

export const useToastStore = defineStore('toast', () => {
  const toasts = ref<Toast[]>([])
  let siguienteId = 0

  function cerrar(id: number) {
    toasts.value = toasts.value.filter((t) => t.id !== id)
  }

  function mostrar(mensaje: string, tipo: TipoToast = 'info', duracionMs = 4000): number {
    const id = ++siguienteId
    toasts.value.push({ id, tipo, mensaje })
    // Los de "cargando" los cierra quien lanzó el proceso.
    if (duracionMs > 0) setTimeout(() => cerrar(id), duracionMs)
    return id
  }

  const exito = (mensaje: string) => mostrar(mensaje, 'exito')
  const error = (mensaje: string) => mostrar(mensaje, 'error', 7000)
  const info = (mensaje: string) => mostrar(mensaje, 'info')
  const cargando = (mensaje: string) => mostrar(mensaje, 'cargando', 0)

  return { toasts, mostrar, exito, error, info, cargando, cerrar }
})
