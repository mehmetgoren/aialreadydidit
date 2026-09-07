import type { RouteRecordRaw } from 'vue-router'

/** Member dashboard — children of /dashboard (DashboardLayout). */
export const dashboardRoutes: RouteRecordRaw[] = [
  { path: '', name: 'dashboard', component: () => import('@/pages/dashboard/OverviewPage.vue'), meta: { titleKey: 'dash_overview' } },
  { path: 'apps', name: 'my-apps', component: () => import('@/pages/dashboard/MyAppsPage.vue'), meta: { titleKey: 'dash_my_apps' } },
  { path: 'apps/:id/stats', name: 'my-app-stats', component: () => import('@/pages/dashboard/AppStatsPage.vue'), meta: { titleKey: 'dash_app_stats' } },
  { path: 'downloads', name: 'my-downloads', component: () => import('@/pages/dashboard/DownloadsPage.vue'), meta: { titleKey: 'dash_downloads' } },
  { path: 'ratings', name: 'my-ratings', component: () => import('@/pages/dashboard/MyRatingsPage.vue'), meta: { titleKey: 'dash_ratings' } },
  { path: 'favorites', name: 'my-favorites', component: () => import('@/pages/dashboard/FavoritesPage.vue'), meta: { titleKey: 'dash_favorites' } },
  { path: 'collections', name: 'my-collections', component: () => import('@/pages/dashboard/CollectionsPage.vue'), meta: { titleKey: 'dash_collections' } },
  { path: 'collections/:id', name: 'my-collection', component: () => import('@/pages/dashboard/CollectionsPage.vue'), meta: { titleKey: 'dash_collections' } },
  { path: 'watches', name: 'my-watches', component: () => import('@/pages/dashboard/WatchesPage.vue'), meta: { titleKey: 'dash_watches' } },
  { path: 'notifications', name: 'my-notifications', component: () => import('@/pages/dashboard/NotificationsPage.vue'), meta: { titleKey: 'dash_notifications' } },
  { path: 'api-keys', name: 'my-api-keys', component: () => import('@/pages/dashboard/ApiKeysPage.vue'), meta: { titleKey: 'dash_api_keys' } },
  { path: 'settings', name: 'my-settings', component: () => import('@/pages/dashboard/SettingsPage.vue'), meta: { titleKey: 'dash_settings' } },
]
