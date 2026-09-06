import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useUserStore } from '@/stores/user-store'
import { localService } from '@/utils/services/local-service'
import type { AuthResponse, MeDto } from '@/utils/models/user-models'

const mocks = vi.hoisted(() => ({ me: vi.fn(), refresh: vi.fn(), signOut: vi.fn() }))
vi.mock('@/utils/services/account-service', () => ({
  AccountService: class {
    me = mocks.me
    refresh = mocks.refresh
    signOut = mocks.signOut
  },
}))

const me = (patch: Partial<MeDto> = {}): MeDto =>
  ({ id: 1, email: 'a@b.c', username: 'ayse', displayName: '', role: 'Member', isAdmin: false, unreadNotificationCount: 0, ...patch }) as MeDto
const auth = (patch: Partial<MeDto> = {}): AuthResponse => ({ token: 'jwt', tokenExpireDate: '2026-09-07T00:00:00Z', user: me(patch) })

describe('user store', () => {
  beforeEach(() => {
    localStorage.clear()
    setActivePinia(createPinia())
    mocks.me.mockReset()
    mocks.refresh.mockReset()
    mocks.signOut.mockReset()
  })

  it('starts from what LocalService persisted', () => {
    localService.setCurrentUser({ token: 'saved', tokenExpireDate: null, user: me({ displayName: 'Ayşe' }) })
    const store = useUserStore()
    expect(store.isAuthenticated).toBe(true)
    expect(store.displayName).toBe('Ayşe')
    expect(store.isAdmin).toBe(false)
  })

  it('applyAuth persists and exposes the member', () => {
    const store = useUserStore()
    expect(store.isAuthenticated).toBe(false)
    expect(store.displayName).toBe('')
    store.applyAuth(auth({ isAdmin: true }))
    expect(store.isAuthenticated).toBe(true)
    expect(store.isAdmin).toBe(true)
    expect(store.displayName).toBe('ayse')
    expect(localService.getCurrentUser()?.token).toBe('jwt')
  })

  it('patchMe merges into the stored profile', () => {
    const store = useUserStore()
    store.patchMe({ displayName: 'nobody' })
    expect(store.me).toBeNull()
    store.applyAuth(auth())
    store.patchMe({ displayName: 'Renamed', unreadNotificationCount: 3 })
    expect(store.me?.displayName).toBe('Renamed')
    expect(store.me?.unreadNotificationCount).toBe(3)
    expect(localService.getCurrentUser()?.user?.displayName).toBe('Renamed')
  })

  it('refreshMe only calls the API when signed in', async () => {
    const store = useUserStore()
    expect(await store.refreshMe()).toBeNull()
    expect(mocks.me).not.toHaveBeenCalled()
    store.applyAuth(auth())
    mocks.me.mockResolvedValue(me({ appCount: 7 }))
    expect((await store.refreshMe())?.appCount).toBe(7)
    expect(store.me?.appCount).toBe(7)
    expect(store.currentUser?.token).toBe('jwt')
  })

  it('refreshToken applies the new token or reports failure', async () => {
    const store = useUserStore()
    mocks.refresh.mockResolvedValueOnce({ ...auth(), token: 'fresh' })
    expect(await store.refreshToken()).toBe(true)
    expect(store.currentUser?.token).toBe('fresh')
    mocks.refresh.mockRejectedValueOnce(new Error('expired'))
    expect(await store.refreshToken()).toBe(false)
    expect(store.currentUser?.token).toBe('fresh')
  })

  it('signOut clears local state even if the API call fails', async () => {
    const store = useUserStore()
    store.applyAuth(auth())
    mocks.signOut.mockRejectedValueOnce(new Error('offline'))
    await store.signOut()
    expect(store.isAuthenticated).toBe(false)
    expect(localService.getCurrentUser()).toBeNull()
  })
})
