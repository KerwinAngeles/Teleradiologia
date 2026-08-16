import { ref, onMounted, onUnmounted } from 'vue'

// Reloj compartido para los contadores de plazo: un solo intervalo en vez de uno por fila.
export function useReloj(intervaloMs = 30_000) {
  const ahora = ref(Date.now())
  let temporizador: ReturnType<typeof setInterval> | null = null

  onMounted(() => {
    temporizador = setInterval(() => (ahora.value = Date.now()), intervaloMs)
  })

  onUnmounted(() => {
    if (temporizador) clearInterval(temporizador)
  })

  return { ahora }
}

export function formatearRestante(fechaLimite: string, ahora: number): string {
  const minutos = Math.round((new Date(fechaLimite).getTime() - ahora) / 60000)
  const vencido = minutos < 0
  const abs = Math.abs(minutos)

  const texto =
    abs < 60
      ? `${abs} min`
      : abs < 1440
        ? `${Math.floor(abs / 60)} h ${abs % 60} min`
        : `${Math.floor(abs / 1440)} d ${Math.floor((abs % 1440) / 60)} h`

  return vencido ? `vencido hace ${texto}` : `faltan ${texto}`
}
