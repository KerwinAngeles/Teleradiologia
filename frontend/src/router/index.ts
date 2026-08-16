import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useNavegacionStore } from '@/stores/navegacion'
import type { Rol } from '@/types/auth'

declare module 'vue-router' {
  interface RouteMeta {
    public?: boolean
    roles?: Rol[]
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/LoginView.vue'),
      meta: { public: true },
    },
    {
      path: '/registro',
      name: 'registro',
      component: () => import('@/views/RegistroView.vue'),
      meta: { public: true },
    },
    {
      path: '/',
      component: () => import('@/layouts/AppLayout.vue'),
      children: [
        {
          path: '',
          name: 'worklist',
          component: () => import('@/views/WorklistView.vue'),
        },
        {
          path: 'estudios/:id',
          name: 'estudio-detalle',
          component: () => import('@/views/EstudioDetalleView.vue'),
          props: true,
        },
        {
          path: 'resultados',
          name: 'resultados-estudio',
          component: () => import('@/views/ResultadosEstudioView.vue'),
          meta: { roles: ['Tecnico', 'Admin'] },
        },
        {
          path: 'usuarios',
          name: 'usuarios',
          component: () => import('@/views/UsuariosView.vue'),
          meta: { roles: ['Admin'] },
        },
        {
          path: 'notificaciones',
          name: 'notificaciones',
          component: () => import('@/views/NotificacionesView.vue'),
          meta: { roles: ['Radiologo', 'Admin'] },
        },
        {
          path: 'configuracion',
          name: 'configuracion',
          component: () => import('@/views/ConfiguracionView.vue'),
          meta: { roles: ['Admin'] },
        },
        {
          path: 'configuracion/hospitales',
          name: 'configuracion-hospitales',
          component: () => import('@/views/ConfiguracionHospitalesView.vue'),
          meta: { roles: ['Admin'] },
        },
        {
          path: 'configuracion/eventos',
          name: 'configuracion-eventos',
          component: () => import('@/views/ConfiguracionEventosView.vue'),
          meta: { roles: ['Admin'] },
        },
        {
          path: 'configuracion/resumen',
          name: 'configuracion-resumen',
          component: () => import('@/views/ConfiguracionResumenView.vue'),
          meta: { roles: ['Admin'] },
        },
      ],
    },
  ],
  scrollBehavior(_to, _from, savedPosition) {
    return savedPosition ?? { top: 0, behavior: 'smooth' }
  },
})

router.beforeEach((to, from) => {
  const auth = useAuthStore()

  if (to.path !== from.path) {
    useNavegacionStore().iniciar()
  }

  if (!to.meta.public && !auth.estaAutenticado) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if ((to.name === 'login' || to.name === 'registro') && auth.estaAutenticado) {
    return { name: 'worklist' }
  }

  if (to.meta.roles && (!auth.usuario || !to.meta.roles.includes(auth.usuario.rol))) {
    return { name: 'worklist' }
  }

  return true
})

router.afterEach(() => useNavegacionStore().terminar())
router.onError(() => useNavegacionStore().terminar())

export default router
