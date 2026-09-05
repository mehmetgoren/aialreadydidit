import type { RouteRecordRaw } from 'vue-router'

export const adminCatalogRoutes: RouteRecordRaw[] = [
  { path: 'apps', name: 'admin-apps', component: () => import('@/pages/admin/catalog/AppsPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_apps' } },
  { path: 'apps/:id', name: 'admin-app', component: () => import('@/pages/admin/catalog/AppDetailPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_apps' } },
  { path: 'categories', name: 'admin-categories', component: () => import('@/pages/admin/catalog/CategoriesPage.vue'), meta: { requiresAdmin: true, titleKey: 'admin_categories' } },
  { path: 'tags', name: 'admin-tags', component: () => import('@/pages/admin/catalog/TagsPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_tags' } },
  { path: 'licenses', name: 'admin-licenses', component: () => import('@/pages/admin/catalog/LicensesPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_licenses' } },
  { path: 'platforms', name: 'admin-platforms', component: () => import('@/pages/admin/catalog/PlatformsPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_platforms' } },
  { path: 'llm-models', name: 'admin-llm-models', component: () => import('@/pages/admin/catalog/LlmModelsPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_llm_models' } },
]
