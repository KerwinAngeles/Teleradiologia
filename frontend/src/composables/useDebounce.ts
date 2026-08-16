import { ref, watch, type Ref } from 'vue'

// Retrasa el valor para no disparar una consulta por cada tecla.
export function useDebounce<T>(fuente: Ref<T>, esperaMs = 350): Ref<T> {
  const salida = ref(fuente.value) as Ref<T>
  let temporizador: ReturnType<typeof setTimeout> | null = null

  watch(fuente, (valor) => {
    if (temporizador) clearTimeout(temporizador)
    temporizador = setTimeout(() => (salida.value = valor), esperaMs)
  })

  return salida
}
