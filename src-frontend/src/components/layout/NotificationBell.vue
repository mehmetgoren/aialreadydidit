<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user-store'
import { useNotificationStore } from '@/stores/notification-store'
import type { NotificationDto } from '@/utils/models/dashboard-models'
import { fromNow } from '@/utils/format'

const { t } = useI18n()
const router = useRouter()
const user = useUserStore()
const store = useNotificationStore()
const visible = ref(false)

onMounted(() => {
  if (user.isAuthenticated) store.startPolling()
})
onUnmounted(() => store.stopPolling())

async function open(row: NotificationDto) {
  visible.value = false
  if (!row.readAt) await store.markRead(row.id).catch(() => undefined)
  if (row.link) await router.push(row.link)
}

async function goAll() {
  visible.value = false
  await router.push('/dashboard/notifications')
}
</script>

<template>
  <ElPopover v-model:visible="visible" trigger="click" placement="bottom-end" :width="360">
    <template #reference>
      <span class="bell">
        <ElBadge :value="store.badge" :hidden="!store.badge" :offset="[2, 4]">
          <ElIcon :size="22"><Bell /></ElIcon>
        </ElBadge>
      </span>
    </template>
    <div class="bell__head">
      <strong>{{ t('notifications') }}</strong>
      <ElButton v-if="store.unread" text size="small" @click="store.markAllRead()">{{ t('mark_all_read') }}</ElButton>
    </div>
    <div v-if="!store.latest.length" class="gm-muted bell__empty">{{ t('no_notifications') }}</div>
    <div v-for="n in store.latest" :key="n.id" class="bell__row" :class="{ 'is-unread': !n.readAt }" @click="open(n)">
      <div class="bell__title">{{ n.title }}</div>
      <div v-if="n.body" class="bell__body">{{ n.body }}</div>
      <div class="bell__time">{{ fromNow(n.createdAt) }}</div>
    </div>
    <div class="bell__foot"><ElButton text type="primary" @click="goAll">{{ t('see_all') }}</ElButton></div>
  </ElPopover>
</template>

<style scoped lang="scss">
.bell {
  display: inline-flex;
  cursor: pointer;
  color: var(--gm-text);
  &__head {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 8px;
  }
  &__empty {
    padding: 16px 0;
    text-align: center;
  }
  &__row {
    padding: 8px 6px;
    border-top: 1px solid var(--gm-border);
    cursor: pointer;
    &.is-unread {
      background: var(--gm-primary-light);
    }
    &:hover {
      background: rgba(0, 0, 0, 0.03);
    }
  }
  &__title {
    font-weight: 600;
    font-size: 13px;
  }
  &__body {
    font-size: 12px;
    color: var(--gm-text-muted);
    overflow: hidden;
    text-overflow: ellipsis;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
  }
  &__time {
    font-size: 11px;
    color: var(--gm-text-muted);
  }
  &__foot {
    text-align: center;
    margin-top: 6px;
  }
}
</style>
