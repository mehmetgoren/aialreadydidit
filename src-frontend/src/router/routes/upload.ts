import type { RouteRecordRaw } from 'vue-router'

/** The upload wizard / app editor (signed-in members). */
export const uploadRoutes: RouteRecordRaw[] = [
  { path: 'upload', name: 'upload', component: () => import('@/pages/dashboard/AppEditorPage.vue'), meta: { titleKey: 'upload_app' } },
  { path: 'upload/:id', name: 'upload-edit', component: () => import('@/pages/dashboard/AppEditorPage.vue'), meta: { titleKey: 'upload_app' } },
]
