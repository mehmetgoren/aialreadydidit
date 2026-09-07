import type { RouteRecordRaw } from 'vue-router'

export const adminModerationRoutes: RouteRecordRaw[] = [
  { path: 'moderation', name: 'admin-moderation', component: () => import('@/pages/admin/moderation/QueuePage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_moderation_queue' } },
  { path: 'moderation/:appId', name: 'admin-review', component: () => import('@/pages/admin/moderation/ReviewPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_review' } },
  { path: 'reports', name: 'admin-reports', component: () => import('@/pages/admin/moderation/ReportsPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_reports' } },
  { path: 'requests', name: 'admin-requests', component: () => import('@/pages/admin/moderation/RequestsPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_requests' } },
]
