import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useNotificationStore } from '@/stores/notification-store'
import { useUserStore } from '@/stores/user-store'
import type { MeDto } from '@/utils/models/user-models'
import type { NotificationDto } from '@/utils/models/dashboard-models'

const mocks = vi.hoisted(() => ({ notifications: vi.fn(), markRead: vi.fn(), markAllRead: vi.fn(), me: vi.fn() }))
vi.mock('@/utils/services/dashboard-service', () => ({
  DashboardService: class {
    notifications = mocks.notifications
    markRead = mocks.markRead
    markAllRead = mocks.markAllRead
  },
}))
vi.mock('@/utils/services/account-service', () => ({
  AccountService: class {
    me = mocks.me
    refresh = vi.fn()
    signOut = vi.fn()
  },
}))

const row = (id: number, readAt: string | null = null) => ({ id, readAt, title: `n${id}` }) as unknown as NotificationDto
const meDto = (unread: number): MeDto => ({ id: 1, username: 'u', displayName: 'U', role: 'Member', isAdmin: false, unreadNotificationCount: unread }) as MeDto

describe('notification store', () => {
  beforeEach(() => {
    localStorage.clear()
    setActivePinia(createPinia())
    Object.values(mocks).forEach((m) => m.mockReset())
    mocks.markRead.mockResolvedValue({ ok: true })
    mocks.markAllRead.mockResolvedValue({ ok: true })
  })
  afterEach(() => {
    vi.useRealTimers()
  })

  it('does nothing while signed out', async () => {
    const store = useNotificationStore()
    await store.fetchLatest()
    expect(mocks.notifications).not.toHaveBeenCalled()
    expect(store.badge).toBeUndefined()
  })

  it('loads the latest rows and trusts the server unread counter', async () => {
    useUserStore().applyAuth({ token: 't', tokenExpireDate: '', user: meDto(0) })
    mocks.notifications.mockResolvedValue({ items: [row(1), row(2, '2026-09-01'), row(3)] })
    mocks.me.mockResolvedValue(meDto(120))
    const store = useNotificationStore()
    await store.fetchLatest()
    expect(store.latest).toHaveLength(3)
    expect(store.unread).toBe(120)
    expect(store.badge).toBe('99+')
  })

  it('markRead / markAllRead update rows, counter and the header profile', async () => {
    const user = useUserStore()
    user.applyAuth({ token: 't', tokenExpireDate: '', user: meDto(2) })
    mocks.notifications.mockResolvedValue({ items: [row(1), row(2)] })
    mocks.me.mockResolvedValue(meDto(2))
    const store = useNotificationStore()
    await store.fetchLatest()
    expect(store.badge).toBe(2)

    await store.markRead(1)
    expect(mocks.markRead).toHaveBeenCalledWith(1)
    expect(store.latest[0]?.readAt).toBeTruthy()
    expect(store.unread).toBe(1)
    expect(user.me?.unreadNotificationCount).toBe(1)
    await store.markRead(1)
    expect(store.unread).toBe(1)

    await store.markAllRead()
    expect(store.latest.every((n) => n.readAt)).toBe(true)
    expect(store.unread).toBe(0)
    expect(store.badge).toBeUndefined()
    expect(user.me?.unreadNotificationCount).toBe(0)
  })

  it('polls every minute while signed in and stops cleanly', async () => {
    vi.useFakeTimers()
    useUserStore().applyAuth({ token: 't', tokenExpireDate: '', user: meDto(0) })
    mocks.notifications.mockResolvedValue({ items: [] })
    mocks.me.mockResolvedValue(meDto(0))
    const store = useNotificationStore()
    store.startPolling()
    store.startPolling()
    expect(mocks.notifications).toHaveBeenCalledTimes(1)
    await vi.advanceTimersByTimeAsync(60_000)
    expect(mocks.notifications).toHaveBeenCalledTimes(2)
    store.stopPolling()
    await vi.advanceTimersByTimeAsync(120_000)
    expect(mocks.notifications).toHaveBeenCalledTimes(2)
  })
})
