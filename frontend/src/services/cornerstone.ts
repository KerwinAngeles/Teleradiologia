import { init as coreInit } from '@cornerstonejs/core'
import { init as dicomImageLoaderInit } from '@cornerstonejs/dicom-image-loader'
import {
  init as toolsInit,
  addTool,
  LengthTool,
  PanTool,
  StackScrollTool,
  WindowLevelTool,
  ZoomTool,
} from '@cornerstonejs/tools'
import { useAuthStore } from '@/stores/auth'

// Una sola init por sesión: levanta workers y registra los decodificadores WASM.
let inicializacion: Promise<void> | null = null

export function inicializarCornerstone(): Promise<void> {
  inicializacion ??= (async () => {
    await coreInit()

    dicomImageLoaderInit({
      // El loader usa XHR propio: el interceptor de Axios no aplica acá.
      beforeSend(): Record<string, string> {
        const auth = useAuthStore()
        return auth.token ? { Authorization: `Bearer ${auth.token}` } : {}
      },
      // Loader clásico: el provider nuevo falla con píxeles sin comprimir
      // ("no pixel data in NATURALIZED"). Deprecado: revisar al actualizar Cornerstone.
      useLegacyMetadataProvider: true,
    })

    await toolsInit()

    addTool(WindowLevelTool)
    addTool(PanTool)
    addTool(ZoomTool)
    addTool(StackScrollTool)
    addTool(LengthTool)
  })()

  return inicializacion
}

// wadouri: descarga el DICOM entero y lo parsea en el cliente, con su profundidad real.
export function imageIdDeCorte(estudioId: string, orthancInstanceId: string): string {
  return `wadouri:/api/estudios/${estudioId}/imagenes/${orthancInstanceId}/dicom`
}
