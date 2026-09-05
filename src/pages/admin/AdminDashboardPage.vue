<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { AdminDashboard } from '@/utils/models/admin-models'
import { AdminPanelService } from '@/utils/services/admin-service'
import AdminPage from '@/components/admin/AdminPage.vue'
import AdminStatCard from '@/components/admin/AdminStatCard.vue'
import AdminBarChart from '@/components/admin/AdminBarChart.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import SavingsCounter from '@/components/common/SavingsCounter.vue'
import { formatNumber, fromNow } from '@/utils/format'
import { notifyError } from '@/utils/tools'

const { t } = useI18n()
const data = ref<AdminDashboard | null>(null)
const loading = ref(true)

onMounted(async () => {
  try {
    data.value = await new AdminPanelService().dashboard()
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <AdminPage :title="t('admin_dashboard')" :loading="loading">
    <template v-if="data">
      <div class="dash__stats">
        <AdminStatCard :label="t('adm_pending_review')" :value="data.pendingReview" icon="Checked" :tone="data.pendingReview ? 'warning' : 'default'" to="/admin/moderation" />
        <AdminStatCard :label="t('adm_pending_scan')" :value="data.pendingScan" icon="Loading" to="/admin/moderation?status=scan" />
        <AdminStatCard :label="t('adm_open_reports')" :value="data.openReports" icon="WarningFilled" :tone="data.openReports ? 'danger' : 'default'" to="/admin/reports" />
        <AdminStatCard :label="t('adm_open_requests')" :value="data.openRequests" icon="QuestionFilled" to="/admin/requests" />
        <AdminStatCard :label="t('adm_published_apps')" :value="data.publishedApps" :hint="t('adm_of_total', { n: data.totalApps })" icon="Box" tone="success" to="/admin/apps" />
        <AdminStatCard :label="t('app_users')" :value="data.members" :hint="t('adm_new_7d', { n: data.newMembers7d })" icon="User" to="/admin/users" />
        <AdminStatCard :label="t('downloads')" :value="formatNumber(data.downloads)" :hint="t('adm_last_7d', { n: data.downloads7d })" icon="Download" tone="success" />
        <AdminStatCard :label="t('adm_searches')" :value="formatNumber(data.searches)" :hint="t('adm_zero_results_7d', { n: data.zeroResultSearches7d, agents: data.agentCalls7d })" icon="Search" to="/admin/search-analytics" />
        <AdminStatCard :label="t('adm_system_jobs')" :value="data.queuedJobs" :hint="data.failedJobs ? t('adm_failed_jobs', { n: data.failedJobs }) : ''" icon="Timer" :tone="data.failedJobs ? 'danger' : 'default'" to="/admin/jobs" />
        <AdminStatCard :label="t('adm_infected_files')" :value="data.infectedFiles" icon="CircleCloseFilled" :tone="data.infectedFiles ? 'danger' : 'default'" />
        <AdminStatCard :label="t('adm_proposed_categories')" :value="data.proposedCategories" icon="Files" :tone="data.proposedCategories ? 'warning' : 'default'" to="/admin/categories" />
      </div>
      <SavingsCounter :savings="data.savings" compact style="margin: 16px 0" />
      <div class="dash__charts">
        <div class="gm-card dash__chart"><h4>{{ t('downloads') }} · 30d</h4><AdminBarChart :labels="data.downloadsSeries.map((d) => d.date.slice(5))" :series="[{ label: t('downloads'), values: data.downloadsSeries.map((d) => d.value) }]" :height="180" /></div>
        <div class="gm-card dash__chart"><h4>{{ t('adm_searches') }} · 30d</h4><AdminBarChart type="line" :labels="data.searchesSeries.map((d) => d.date.slice(5))" :series="[{ label: t('adm_searches'), values: data.searchesSeries.map((d) => d.value) }, { label: t('adm_signups'), values: data.signupsSeries.map((d) => d.value) }]" :height="180" /></div>
      </div>
      <div class="dash__cols">
        <div class="gm-card dash__box">
          <div class="gm-section__head"><h4>{{ t('adm_moderation_queue') }}</h4><RouterLink to="/admin/moderation" class="gm-link">{{ t('see_all') }}</RouterLink></div>
          <div v-if="!data.queuePreview.length" class="gm-muted">{{ t('adm_queue_empty') }}</div>
          <div v-for="q in data.queuePreview" :key="q.appId" class="dash__row">
            <RouterLink :to="`/admin/moderation/${q.appId}`" class="gm-link"><strong>{{ q.name }}</strong></RouterLink>
            <span class="sub">{{ q.kind === 'version' ? `v${q.version}` : t('adm_new_app') }} · {{ q.uploaderUsername }} · {{ fromNow(q.submittedAt) }}</span>
            <StatusTag :value="q.versionStatus ?? q.appStatus" />
          </div>
        </div>
        <div class="gm-card dash__box">
          <div class="gm-section__head"><h4>{{ t('adm_reports') }}</h4><RouterLink to="/admin/reports" class="gm-link">{{ t('see_all') }}</RouterLink></div>
          <div v-if="!data.recentReports.length" class="gm-muted">{{ t('adm_no_reports') }}</div>
          <div v-for="r in data.recentReports" :key="r.id" class="dash__row">
            <RouterLink to="/admin/reports" class="gm-link"><strong>{{ r.appName ?? '—' }}</strong></RouterLink>
            <span class="sub">{{ t(`report_reason_${r.reason}`) }} · {{ fromNow(r.createdAt) }}</span>
            <StatusTag :value="r.status" />
          </div>
        </div>
        <div class="gm-card dash__box">
          <div class="gm-section__head"><h4>{{ t('adm_top_apps') }}</h4></div>
          <div v-for="a in data.topApps" :key="a.id" class="dash__row">
            <RouterLink :to="`/app/${a.slug}`" class="gm-link"><strong>{{ a.name }}</strong></RouterLink>
            <span class="sub">{{ formatNumber(a.downloadCount) }} {{ t('downloads').toLowerCase() }}</span>
          </div>
        </div>
      </div>
    </template>
  </AdminPage>
</template>

<style scoped lang="scss">
.dash {
  &__stats { display: grid; grid-template-columns: repeat(auto-fill, minmax(190px, 1fr)); gap: 10px; }
  &__charts { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
  &__chart { padding: 12px 14px; h4 { margin: 0 0 6px; } }
  &__cols { display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; margin-top: 12px; }
  &__box { padding: 12px 14px; h4 { margin: 0; } }
  &__row { display: flex; flex-direction: column; gap: 2px; padding: 6px 0; border-top: 1px solid var(--gm-border); align-items: flex-start; }
  @media (max-width: 1000px) { &__charts, &__cols { grid-template-columns: 1fr; } }
}
</style>
