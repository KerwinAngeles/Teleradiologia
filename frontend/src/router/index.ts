import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
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
          path: 'subir',
          name: 'subir-estudio',
          component: () => import('@/views/SubirEstudioView.vue'),
          meta: { roles: ['Tecnico', 'Admin'] },
        },
        {
          path: 'usuarios',
          name: 'usuarios',
          component: () => import('@/views/UsuariosView.vue'),
          meta: { roles: ['Admin'] },
        },
      ],
    },
  ],
  scrollBehavior(_to, _from, savedPosition) {
    return savedPosition ?? { top: 0, behavior: 'smooth' }
  },
})

router.beforeEach((to) => {
  const auth = useAuthStore()

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

export default router
