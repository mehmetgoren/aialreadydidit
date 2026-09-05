<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { DashboardOverview } from '@/utils/models/dashboard-models'
import { DashboardService } from '@/utils/services/dashboard-service'
import { useUserStore } from '@/stores/user-store'
import { useCommonStore } from '@/stores/common-store'
import AdminStatCard from '@/components/admin/AdminStatCard.vue'
import AppCard from '@/components/catalog/AppCard.vue'
import { formatMoney, formatNumber, fromNow } from '@/utils/format'
import { notifyError } from '@/utils/tools'

const { t } = useI18n()
const user = useUserStore()
const common = useCommonStore()
const data = ref<DashboardOverview | null>(null)
const loading = ref(true)

onMounted(async () => {
  common.setPageTitle(t('dash_overview'))
  try {
    data.value = await new DashboardService().overview()
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div v-loading="loading">
    <h2 class="gm-title">{{ t('hello_name', { name: user.displayName }) }}</h2>
    <ElAlert v-if="user.me && !user.me.emailVerified" type="warning" :closable="false" show-icon :title="t('email_not_verified')" style="margin-bottom: 14px" />
    <template v-if="data">
      <div class="ov__stats">
        <AdminStatCard :label="t('dash_my_apps')" :value="data.publishedAppCount" :hint="data.pendingAppCount ? t('n_pending', { n: data.pendingAppCount }) : ''" icon="Box" to="/dashboard/apps" />
        <AdminStatCard :label="t('stat_downloads')" :value="formatNumber(data.totalDownloads)" icon="Download" tone="success" />
        <AdminStatCard :label="t('savings_by_my_apps')" :value="formatNumber(data.tokensSavedByMyApps)" :hint="'≈ ' + formatMoney(data.costSavedByMyApps)" icon="Coin" tone="purple" />
        <AdminStatCard :label="t('dash_favorites')" :value="data.favoriteCount" icon="Star" tone="warning" to="/dashboard/favorites" />
        <AdminStatCard :label="t('dash_api_keys')" :value="data.apiKeyCount" icon="Key" to="/dashboard/api-keys" />
        <AdminStatCard :label="t('dash_notifications')" :value="data.unreadNotifications" icon="Bell" to="/dashboard/notifications" />
      </div>
      <div class="ov__cols">
        <section>
          <div class="gm-section__head"><h2>{{ t('recent_apps') }}</h2><RouterLink to="/upload" class="gm-link">{{ t('upload_app') }} →</RouterLink></div>
          <div v-if="data.recentApps.length" class="gm-grid"><AppCard v-for="a in data.recentApps" :key="a.id" :app="a" /></div>
          <div v-else class="gm-muted">{{ t('no_apps_yet') }} <RouterLink to="/upload" class="gm-link">{{ t('upload_first') }}</RouterLink></div>
        </section>
        <section>
          <div class="gm-section__head"><h2>{{ t('dash_notifications') }}</h2></div>
          <div v-if="!data.recentNotifications.length" class="gm-muted">{{ t('no_notifications') }}</div>
          <div v-for="n in data.recentNotifications" :key="n.id" class="ov__notif" :class="{ 'is-unread': !n.readAt }">
            <RouterLink :to="n.link || '/dashboard/notifications'"><strong>{{ n.title }}</strong></RouterLink>
            <div class="sub">{{ fromNow(n.createdAt) }}</div>
          </div>
        </section>
      </div>
    </template>
  </div>
</template>

<style scoped lang="scss">
.ov {
  &__stats { display: grid; grid-template-columns: repeat(auto-fill, minmax(190px, 1fr)); gap: 12px; margin-bottom: 20px; }
  &__cols { display: grid; grid-template-columns: 1.4fr 1fr; gap: 20px; }
  &__notif { padding: 8px 10px; border-radius: 8px; margin-bottom: 4px; &.is-unread { background: var(--gm-primary-light); } a { color: var(--gm-text); } }
  @media (max-width: 900px) { &__cols { grid-template-columns: 1fr; } }
}
</style>
