import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export type Tema = 'claro' | 'oscuro' | 'sistema'

const STORAGE_KEY = 'teleradiologia.tema'

function preferenciaDelSistema(): 'claro' | 'oscuro' {
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'oscuro' : 'claro'
}

export const useTemaStore = defineStore('tema', () => {
  const guardado = localStorage.getItem(STORAGE_KEY) as Tema | null
  const preferencia = ref<Tema>(guardado ?? 'sistema')

  const efectivo = computed<'claro' | 'oscuro'>(() =>
    preferencia.value === 'sistema' ? preferenciaDelSistema() : preferencia.value,
  )

  const esOscuro = computed(() => efectivo.value === 'oscuro')

  function aplicar() {
    const raiz = document.documentElement

    // Sin atributo el CSS sigue a prefers-color-scheme; con atributo, gana la elección.
    if (preferencia.value === 'sistema') {
      raiz.removeAttribute('data-theme')
    } else {
      raiz.setAttribute('data-theme', preferencia.value)
    }

    document
      .querySelector('meta[name="theme-color"]')
      ?.setAttribute('content', esOscuro.value ? '#0d0c11' : '#f4f1f6')
  }

  function establecer(nuevo: Tema) {
    preferencia.value = nuevo

    if (nuevo === 'sistema') {
      localStorage.removeItem(STORAGE_KEY)
    } else {
      localStorage.setItem(STORAGE_KEY, nuevo)
    }

    aplicar()
  }

  function alternar() {
    establecer(esOscuro.value ? 'claro' : 'oscuro')
  }

  // Si nunca eligió, sigue los cambios del sistema en vivo.
  window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
    if (preferencia.value === 'sistema') aplicar()
  })

  aplicar()

  return { preferencia, efectivo, esOscuro, establecer, alternar }
})
