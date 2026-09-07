import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import type { NotificationDto } from '@/utils/models/dashboard-models'
import { DashboardService } from '@/utils/services/dashboard-service'
import { useUserStore } from '@/stores/user-store'

const service = new DashboardService()
const POLL_INTERVAL = 60_000

/** Header bell state: unread count + the newest rows, refreshed every 60 s while signed in. */
export const useNotificationStore = defineStore('notification', () => {
  const latest = ref<NotificationDto[]>([])
  const unread = ref(0)
  const loading = ref(false)
  let timer: ReturnType<typeof setInterval> | null = null

  const badge = computed(() => (unread.value > 99 ? '99+' : unread.value || undefined))

  async function fetchLatest() {
    const user = useUserStore()
    if (!user.isAuthenticated) return
    loading.value = true
    try {
      const page = await service.notifications(false, 1, 8)
      latest.value = page.items
      unread.value = page.items.filter((n) => !n.readAt).length
      const me = await user.refreshMe()
      if (me) unread.value = me.unreadNotificationCount
    } catch {
      /* ignore */
    } finally {
      loading.value = false
    }
  }

  async function markRead(id: number) {
    await service.markRead(id)
    const row = latest.value.find((n) => n.id === id)
    if (row && !row.readAt) {
      row.readAt = new Date().toISOString()
      unread.value = Math.max(0, unread.value - 1)
      useUserStore().patchMe({ unreadNotificationCount: unread.value })
    }
  }

  async function markAllRead() {
    await service.markAllRead()
    latest.value.forEach((n) => (n.readAt ??= new Date().toISOString()))
    unread.value = 0
    useUserStore().patchMe({ unreadNotificationCount: 0 })
  }

  function startPolling() {
    if (timer) return
    void fetchLatest()
    timer = setInterval(() => {
      if (useUserStore().isAuthenticated) void fetchLatest()
    }, POLL_INTERVAL)
  }

  function stopPolling() {
    if (!timer) return
    clearInterval(timer)
    timer = null
  }

  return { latest, unread, badge, loading, fetchLatest, markRead, markAllRead, startPolling, stopPolling }
})
