import type { RouteRecordRaw } from 'vue-router'
import { adminModerationRoutes } from './admin/moderation'
import { adminCatalogRoutes } from './admin/catalog'
import { adminMembersRoutes } from './admin/members'
import { adminContentRoutes } from './admin/content'
import { adminStatsRoutes } from './admin/stats'
import { adminSystemRoutes } from './admin/system'

/** Admin panel routes — children of /admin (AdminLayout). Each area keeps its records in ./admin/<area>.ts. */
export const adminRoutes: RouteRecordRaw[] = [
  { path: '', name: 'admin-dashboard', component: () => import('@/pages/admin/AdminDashboardPage.vue'), meta: { requiresAdmin: true, titleKey: 'admin_dashboard' } },
  ...adminModerationRoutes,
  ...adminCatalogRoutes,
  ...adminMembersRoutes,
  ...adminContentRoutes,
  ...adminStatsRoutes,
  ...adminSystemRoutes,
]
