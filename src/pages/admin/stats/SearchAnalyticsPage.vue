<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import AdminBarChart from '@/components/admin/AdminBarChart.vue'
import AdminStatCard from '@/components/admin/AdminStatCard.vue'
import { AdminSystemService } from '@/utils/services/admin-service'
import type { SearchAnalytics } from '@/utils/models/admin-models'
import { notifyError } from '@/utils/tools'
import { formatDateTime, formatNumber } from '@/utils/format'

const { t } = useI18n()
const service = new AdminSystemService()
const days = ref(30)
const data = ref<SearchAnalytics | null>(null)
const loading = ref(true)
async function load() { loading.value = true; data.value = await service.searchAnalytics(days.value).catch((e) => { notifyError(e); return null }); loading.value = false }
onMounted(load)
watch(days, load)
</script>

<template>
  <AdminPage :title="t('adm_stats_search')" :subtitle="t('adm_search_subtitle')" :loading="loading">
    <template #actions><ElRadioGroup v-model="days" size="small"><ElRadioButton :value="7">7d</ElRadioButton><ElRadioButton :value="30">30d</ElRadioButton><ElRadioButton :value="90">90d</ElRadioButton></ElRadioGroup></template>
    <template v-if="data">
      <div class="sa__cards">
        <AdminStatCard :label="t('adm_searches')" :value="formatNumber(data.total)" icon="Search" />
        <AdminStatCard :label="t('adm_avg_latency')" :value="`${Math.round(data.avgTookMs)} ms`" icon="Timer" />
        <AdminStatCard v-for="s in data.bySource" :key="s.value" :label="s.label" :value="formatNumber(s.count)" icon="Connection" />
        <AdminStatCard v-for="m in data.byMode" :key="m.value" :label="m.label" :value="formatNumber(m.count)" icon="MagicStick" tone="purple" />
      </div>
      <div class="gm-card sa__box"><AdminBarChart :labels="data.series.map((d) => d.date.slice(5))" :series="[{ label: t('adm_searches'), values: data.series.map((d) => d.value) }]" :height="160" /></div>
      <div class="sa__cols">
        <div class="gm-card sa__box">
          <h4>{{ t('adm_top_queries') }}</h4>
          <ElTable :data="data.topQueries" size="small" class="gm-table" max-height="480">
            <ElTableColumn prop="query" :label="t('adm_query')" min-width="200"><template #default="{ row }"><RouterLink :to="{ path: '/search', query: { q: row.query } }" target="_blank" class="gm-link">{{ row.query }}</RouterLink></template></ElTableColumn>
            <ElTableColumn prop="count" label="#" width="60" align="right" />
            <ElTableColumn prop="avgResults" :label="t('adm_avg_results')" width="90" align="right" />
            <ElTableColumn :label="t('adm_top_sim')" width="90" align="right"><template #default="{ row }">{{ row.avgTopSimilarity != null ? Math.round(row.avgTopSimilarity * 100) + '%' : '—' }}</template></ElTableColumn>
            <ElTableColumn :label="t('adm_last_seen')" width="140"><template #default="{ row }">{{ formatDateTime(row.lastSeen) }}</template></ElTableColumn>
          </ElTable>
        </div>
        <div class="gm-card sa__box">
          <h4>{{ t('adm_zero_result_queries') }}</h4>
          <p class="sub">{{ t('adm_zero_hint') }}</p>
          <ElTable :data="data.zeroResultQueries" size="small" class="gm-table" max-height="440">
            <ElTableColumn prop="query" :label="t('adm_query')" min-width="200" />
            <ElTableColumn prop="count" label="#" width="60" align="right" />
            <ElTableColumn :label="t('adm_last_seen')" width="140"><template #default="{ row }">{{ formatDateTime(row.lastSeen) }}</template></ElTableColumn>
          </ElTable>
        </div>
      </div>
    </template>
  </AdminPage>
</template>

<style scoped>
.sa__cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(170px, 1fr)); gap: 10px; margin-bottom: 12px; }
.sa__box { padding: 12px 14px; margin-bottom: 12px; }
.sa__box h4 { margin: 0 0 8px; }
.sa__cols { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
@media (max-width: 1000px) { .sa__cols { grid-template-columns: 1fr; } }
</style>
