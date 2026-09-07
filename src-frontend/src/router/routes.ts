import type { RouteRecordRaw } from 'vue-router'
import { catalogRoutes } from './routes/catalog'
import { accountRoutes } from './routes/account'
import { dashboardRoutes } from './routes/dashboard'
import { uploadRoutes } from './routes/upload'
import { adminRoutes } from './routes/admin'

/**
 * Route tree. Each feature keeps its own records in `./routes/<feature>.ts`; this file only
 * decides which layout hosts them.
 */
export const routes: RouteRecordRaw[] = [
  {
    path: '/',
    component: () => import('@/layouts/StorefrontLayout.vue'),
    children: [
      ...catalogRoutes,
      ...accountRoutes,
      ...uploadRoutes,
      {
        path: 'dashboard',
        component: () => import('@/layouts/DashboardLayout.vue'),
        children: dashboardRoutes,
      },
    ],
  },
  {
    path: '/admin',
    component: () => import('@/layouts/AdminLayout.vue'),
    meta: { requiresAdmin: true },
    children: adminRoutes,
  },
  {
    path: '/:catchAll(.*)*',
    name: 'not-found',
    component: () => import('@/pages/ErrorNotFound.vue'),
    meta: { public: true },
  },
]
