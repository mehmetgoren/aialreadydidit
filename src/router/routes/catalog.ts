import type { RouteRecordRaw } from 'vue-router'

/** Public storefront routes (relative to StorefrontLayout). */
export const catalogRoutes: RouteRecordRaw[] = [
  { path: '', name: 'home', component: () => import('@/pages/catalog/HomePage.vue'), meta: { public: true, titleKey: 'app_name' } },
  { path: 'category/:slug', name: 'category', component: () => import('@/pages/catalog/CategoryPage.vue'), meta: { public: true, titleKey: 'categories' } },
  { path: 'search', name: 'search', component: () => import('@/pages/catalog/SearchPage.vue'), meta: { public: true, titleKey: 'search' } },
  { path: 'app/:slug', name: 'app', component: () => import('@/pages/catalog/AppPage.vue'), meta: { public: true, titleKey: 'app' } },
  { path: 'u/:username', name: 'uploader', component: () => import('@/pages/catalog/UploaderPage.vue'), meta: { public: true, titleKey: 'uploader' } },
  { path: 'u/:username/collections/:slug', name: 'collection', component: () => import('@/pages/catalog/CollectionPage.vue'), meta: { public: true, titleKey: 'collection' } },
  { path: 'wanted', name: 'wanted', component: () => import('@/pages/catalog/WantedPage.vue'), meta: { public: true, titleKey: 'wanted' } },
  { path: 'wanted/:id', name: 'wanted-detail', component: () => import('@/pages/catalog/WantedPage.vue'), meta: { public: true, titleKey: 'wanted' } },
  { path: 'about', name: 'about', component: () => import('@/pages/catalog/AboutPage.vue'), meta: { public: true, titleKey: 'about' } },
  { path: 'for-agents', name: 'for-agents', component: () => import('@/pages/catalog/ForAgentsPage.vue'), meta: { public: true, titleKey: 'for_agents' } },
]
