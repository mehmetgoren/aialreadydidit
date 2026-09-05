import type { RouteRecordRaw } from 'vue-router'

export const adminMembersRoutes: RouteRecordRaw[] = [
  { path: 'users', name: 'admin-users', component: () => import('@/pages/admin/members/UsersPage.vue'), meta: { requiresAdmin: true, titleKey: 'app_users' } },
  { path: 'users/:id', name: 'admin-user', component: () => import('@/pages/admin/members/UserDetailPage.vue'), meta: { requiresAdmin: true, titleKey: 'app_users' } },
  { path: 'api-keys', name: 'admin-api-keys', component: () => import('@/pages/admin/members/ApiKeysPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_api_keys' } },
  { path: 'sessions', name: 'admin-sessions', component: () => import('@/pages/admin/members/SessionsPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_identity_sessions' } },
  { path: 'roles', name: 'admin-roles', component: () => import('@/pages/admin/members/RolesPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_identity_roles' } },
  { path: 'menus', name: 'admin-menus', component: () => import('@/pages/admin/members/MenusPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_identity_menus' } },
  { path: 'role-menus', name: 'admin-role-menus', component: () => import('@/pages/admin/members/RoleMenusPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_identity_role_menus' } },
  { path: 'role-actions', name: 'admin-role-actions', component: () => import('@/pages/admin/members/RoleActionsPage.vue'), meta: { requiresAdmin: true, titleKey: 'adm_identity_role_actions' } },
]
