import { createRouter, createWebHistory } from 'vue-router'
import { routes } from './routes'
import { useUserStore } from '@/stores/user-store'
import { setUnauthorizedHandler } from '@/boot/axios'

declare module 'vue-router' {
  interface RouteMeta {
    /** Reachable without signing in. */
    public?: boolean
    /** Only members whose role is admin (or with delegated admin menus). */
    requiresAdmin?: boolean
    /** i18n key for the document title / breadcrumb. */
    titleKey?: string
    /** Static breadcrumb (page may override via commonStore). */
    breadcrumb?: { labelKey: string; to?: string }[]
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  scrollBehavior(to, from, saved) {
    if (saved) return saved
    if (to.path === from.path) return undefined
    return { left: 0, top: 0 }
  },
})

router.beforeEach((to) => {
  const user = useUserStore()
  if (!to.meta.public && !user.isAuthenticated) {
    return { path: '/login', query: { redirect: to.fullPath } }
  }
  if (to.meta.requiresAdmin && !user.isAdmin && user.me?.role !== 'Moderator') {
    return { path: '/' }
  }
  if ((to.path === '/login' || to.path === '/signup') && user.isAuthenticated) {
    return { path: '/' }
  }
})

setUnauthorizedHandler(() => {
  const user = useUserStore()
  user.setCurrentUser(null)
  const current = router.currentRoute.value
  if (!current.meta.public && current.path !== '/login')
    router.push({ path: '/login', query: { redirect: current.fullPath } })
})

export default router
