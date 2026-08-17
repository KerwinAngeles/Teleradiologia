import { defineStore } from 'pinia'
import { ref } from 'vue'

// Las vistas se cargan con import dinámico, así que un cambio de pantalla puede tardar.
const RETRASO_MS = 180
const MINIMO_VISIBLE_MS = 450

export const useNavegacionStore = defineStore('navegacion', () => {
  const cargando = ref(false)

  let temporizadorMostrar: ReturnType<typeof setTimeout> | null = null
  let mostradoEn = 0

  function mostrar() {
    cargando.value = true
    mostradoEn = Date.now()
    temporizadorMostrar = null
  }

  // `inmediato` para las pantallas que se sabe que van a tardar —las que precargan
  // antes de navegar—: ahí esperar los 180 ms deja el clic sin respuesta visible.
  function iniciar(inmediato = false) {
    if (temporizadorMostrar) clearTimeout(temporizadorMostrar)

    if (inmediato) {
      mostrar()
      return
    }

    // Si la navegación es instantánea no se muestra nada: el parpadeo molesta más que la espera.
    temporizadorMostrar = setTimeout(mostrar, RETRASO_MS)
  }

  function terminar() {
    if (temporizadorMostrar) {
      clearTimeout(temporizadorMostrar)
      temporizadorMostrar = null
    }

    if (!cargando.value) return

    // Ya visible: se mantiene un mínimo para que no se corte a mitad de la animación.
    const restante = MINIMO_VISIBLE_MS - (Date.now() - mostradoEn)
    if (restante > 0) {
      setTimeout(() => (cargando.value = false), restante)
    } else {
      cargando.value = false
    }
  }

  return { cargando, iniciar, terminar }
})
