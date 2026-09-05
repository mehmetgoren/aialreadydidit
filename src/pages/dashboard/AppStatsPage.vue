<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import type { MyAppStats } from '@/utils/models/apps-models'
import { MyAppsService } from '@/utils/services/my-apps-service'
import AdminBarChart from '@/components/admin/AdminBarChart.vue'
import AdminStatCard from '@/components/admin/AdminStatCard.vue'
import { formatMoney, formatNumber } from '@/utils/format'
import { notifyError } from '@/utils/tools'

const route = useRoute()
const { t } = useI18n()
const stats = ref<MyAppStats | null>(null)
const loading = ref(true)

onMounted(async () => {
  try {
    stats.value = await new MyAppsService().stats(Number(route.params.id))
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div v-loading="loading">
    <div class="gm-section__head"><h2 class="gm-title">{{ t('dash_app_stats') }}</h2><RouterLink to="/dashboard/apps" class="gm-link">← {{ t('dash_my_apps') }}</RouterLink></div>
    <template v-if="stats">
      <div class="st__cards">
        <AdminStatCard :label="t('downloads_30d')" :value="formatNumber(stats.downloads.reduce((s, d) => s + d.count, 0))" icon="Download" tone="success" />
        <AdminStatCard :label="t('savings_by_my_apps')" :value="formatNumber(stats.estSavedTokens)" :hint="'≈ ' + formatMoney(stats.estSavedCostUsd)" icon="Coin" tone="purple" />
      </div>
      <h3>{{ t('downloads_per_day') }}</h3>
      <AdminBarChart :labels="stats.downloads.map((d) => d.date.slice(5))" :series="[{ label: t('downloads'), values: stats.downloads.map((d) => d.count) }]" />
      <div class="st__cols">
        <div><h3>{{ t('by_platform') }}</h3><ul><li v-for="(v, k) in stats.downloadsByPlatform" :key="k"><span>{{ k }}</span><b>{{ v }}</b></li></ul></div>
        <div><h3>{{ t('by_source') }}</h3><ul><li v-for="(v, k) in stats.downloadsBySource" :key="k"><span>{{ k }}</span><b>{{ v }}</b></li></ul></div>
      </div>
    </template>
  </div>
</template>

<style scoped lang="scss">
.st {
  &__cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 12px; margin-bottom: 20px; }
  &__cols { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; ul { list-style: none; padding: 0; margin: 0; li { display: flex; justify-content: space-between; padding: 6px 0; border-bottom: 1px solid var(--gm-border); } } }
}
</style>
