import type { RouteRecordRaw } from 'vue-router'

export const adminContentRoutes: RouteRecordRaw[] = [
  { path: 'featured', name: 'admin-featured', component: () => import('@/pages/admin/content/FeaturedPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_content_featured' } },
  { path: 'banners', name: 'admin-banners', component: () => import('@/pages/admin/content/BannersPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_content_banners' } },
]
