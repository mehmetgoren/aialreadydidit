import type { RouteRecordRaw } from 'vue-router'

export const accountRoutes: RouteRecordRaw[] = [
  { path: 'login', name: 'login', component: () => import('@/pages/account/LoginPage.vue'), meta: { public: true, titleKey: 'login' } },
  { path: 'signup', name: 'signup', component: () => import('@/pages/account/LoginPage.vue'), meta: { public: true, titleKey: 'signup' } },
  { path: 'forgot-password', name: 'forgot-password', component: () => import('@/pages/account/ForgotPasswordPage.vue'), meta: { public: true, titleKey: 'forgot_password' } },
  { path: 'reset-password', name: 'reset-password', component: () => import('@/pages/account/ResetPasswordPage.vue'), meta: { public: true, titleKey: 'reset_password' } },
  { path: 'verify-email', name: 'verify-email', component: () => import('@/pages/account/VerifyEmailPage.vue'), meta: { public: true, titleKey: 'verify_email' } },
]
