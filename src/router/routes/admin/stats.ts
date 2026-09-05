import type { RouteRecordRaw } from 'vue-router'

export const adminStatsRoutes: RouteRecordRaw[] = [
  { path: 'stats', name: 'admin-stats', component: () => import('@/pages/admin/stats/StatsPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_stats_overview' } },
  { path: 'search-analytics', name: 'admin-search-analytics', component: () => import('@/pages/admin/stats/SearchAnalyticsPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_stats_search' } },
  { path: 'savings', name: 'admin-savings', component: () => import('@/pages/admin/stats/SavingsPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_stats_savings' } },
]
