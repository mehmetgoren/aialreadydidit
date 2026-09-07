<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import type { NotificationDto } from '@/utils/models/dashboard-models'
import { DashboardService } from '@/utils/services/dashboard-service'
import { useNotificationStore } from '@/stores/notification-store'
import { useCommonStore } from '@/stores/common-store'
import PagePagination from '@/components/common/PagePagination.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import { notifyError } from '@/utils/tools'
import { fromNow } from '@/utils/format'

const { t } = useI18n()
const router = useRouter()
const common = useCommonStore()
const store = useNotificationStore()
const service = new DashboardService()
const rows = ref<NotificationDto[]>([])
const total = ref(0)
const page = ref(1)
const unreadOnly = ref(false)
const loading = ref(true)

async function load() {
  loading.value = true
  try {
    const p = await service.notifications(unreadOnly.value, page.value)
    rows.value = p.items
    total.value = p.totalCount
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
}
onMounted(() => {
  common.setPageTitle(t('dash_notifications'))
  load()
})
watch([page, unreadOnly], load)

async function open(n: NotificationDto) {
  if (!n.readAt) {
    await store.markRead(n.id).catch(() => undefined)
    n.readAt = new Date().toISOString()
  }
  if (n.link) router.push(n.link)
}
async function markAll() {
  await store.markAllRead()
  load()
}
</script>

<template>
  <div>
    <div class="gm-section__head"><h2 class="gm-title">{{ t('dash_notifications') }}</h2><div><ElCheckbox v-model="unreadOnly">{{ t('unread_only') }}</ElCheckbox> <ElButton size="small" @click="markAll">{{ t('mark_all_read') }}</ElButton></div></div>
    <div v-loading="loading" style="min-height: 80px">
      <EmptyState v-if="!rows.length && !loading" :title="t('no_notifications')" icon="Bell" />
      <div v-for="n in rows" :key="n.id" class="notif" :class="{ 'is-unread': !n.readAt }" @click="open(n)">
        <div class="notif__title">{{ n.title }}</div>
        <div v-if="n.body" class="notif__body">{{ n.body }}</div>
        <div class="sub">{{ fromNow(n.createdAt) }}</div>
      </div>
    </div>
    <PagePagination v-model:page="page" :page-size="20" :total="total" />
  </div>
</template>

<style scoped>
.notif { padding: 10px 14px; border: 1px solid var(--gm-border); border-radius: 10px; margin-bottom: 8px; cursor: pointer; }
.notif.is-unread { background: var(--gm-primary-light); border-color: transparent; }
.notif__title { font-weight: 600; }
.notif__body { color: var(--gm-text-muted); font-size: 13px; margin: 2px 0; }
</style>
