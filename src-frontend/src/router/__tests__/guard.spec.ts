import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import type { MeDto } from '@/utils/models/user-models'

const Page = vi.hoisted(() => ({ template: '<div />' }))
vi.mock('@/router/routes', () => ({
  routes: [
    { path: '/', component: Page, meta: { public: true } },
    { path: '/login', component: Page, meta: { public: true } },
    { path: '/signup', component: Page, meta: { public: true } },
    { path: '/app/:slug', component: Page, meta: { public: true } },
    { path: '/dashboard', component: Page, children: [{ path: 'apps', component: Page }] },
    { path: '/admin', component: Page, meta: { requiresAdmin: true }, children: [{ path: 'users', component: Page, meta: { requiresAdmin: true } }] },
  ],
}))

import router from '@/router'
import { useUserStore } from '@/stores/user-store'

const signIn = (patch: Partial<MeDto> = {}) =>
  useUserStore().applyAuth({ token: 'jwt', tokenExpireDate: null as unknown as string, user: { id: 1, username: 'u', displayName: 'U', role: 'Member', isAdmin: false, ...patch } as MeDto })

describe('router guard', () => {
  beforeEach(async () => {
    window.scrollTo = vi.fn() // jsdom does not implement it; the router's scrollBehavior calls it
    localStorage.clear()
    setActivePinia(createPinia())
    await router.replace('/')
  })

  it('sends anonymous visitors to the login page with a redirect back', async () => {
    await router.push('/dashboard/apps?tab=drafts')
    expect(router.currentRoute.value.path).toBe('/login')
    expect(router.currentRoute.value.query.redirect).toBe('/dashboard/apps?tab=drafts')
  })

  it('lets anonymous visitors browse public pages', async () => {
    await router.push('/app/cpu-z')
    expect(router.currentRoute.value.path).toBe('/app/cpu-z')
  })

  it('lets members into the dashboard but not the admin panel', async () => {
    signIn()
    await router.push('/dashboard')
    expect(router.currentRoute.value.path).toBe('/dashboard')
    await router.push('/admin/users')
    expect(router.currentRoute.value.path).toBe('/')
  })

  it('admits admins and moderators to the admin panel', async () => {
    signIn({ isAdmin: true })
    await router.push('/admin/users')
    expect(router.currentRoute.value.path).toBe('/admin/users')

    localStorage.clear()
    setActivePinia(createPinia())
    signIn({ role: 'Moderator' })
    await router.push('/admin')
    expect(router.currentRoute.value.path).toBe('/admin')
  })

  it('keeps signed-in members away from login and signup', async () => {
    signIn()
    await router.push('/login')
    expect(router.currentRoute.value.path).toBe('/')
    await router.push('/signup')
    expect(router.currentRoute.value.path).toBe('/')
  })
})
