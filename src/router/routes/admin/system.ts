import type { RouteRecordRaw } from 'vue-router'

export const adminSystemRoutes: RouteRecordRaw[] = [
  { path: 'settings', name: 'admin-settings', component: () => import('@/pages/admin/system/SettingsPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_system_settings' } },
  { path: 'audit-log', name: 'admin-audit-log', component: () => import('@/pages/admin/system/AuditLogPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_identity_audit_log' } },
  { path: 'jobs', name: 'admin-jobs', component: () => import('@/pages/admin/system/JobsPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_system_jobs' } },
  { path: 'health', name: 'admin-health', component: () => import('@/pages/admin/system/HealthPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_system_health' } },
]
