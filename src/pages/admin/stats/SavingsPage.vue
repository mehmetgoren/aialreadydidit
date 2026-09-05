<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import AdminBarChart from '@/components/admin/AdminBarChart.vue'
import SavingsCounter from '@/components/common/SavingsCounter.vue'
import { AdminSystemService } from '@/utils/services/admin-service'
import type { SavingsBreakdown } from '@/utils/models/admin-models'
import { notifyError } from '@/utils/tools'
import { formatMoney, formatNumber } from '@/utils/format'

const { t } = useI18n()
const data = ref<SavingsBreakdown | null>(null)
const loading = ref(true)
onMounted(async () => { data.value = await new AdminSystemService().savings().catch((e) => { notifyError(e); return null }); loading.value = false })
</script>

<template>
  <AdminPage :title="t('adm_stats_savings')" :subtitle="t('adm_savings_subtitle')" :loading="loading">
    <template #actions><RouterLink to="/admin/settings"><ElButton>{{ t('adm_edit_coefficients') }}</ElButton></RouterLink></template>
    <template v-if="data">
      <SavingsCounter :savings="data.totals" compact />
      <div class="gm-card sv__box"><h4>{{ t('adm_tokens_saved_per_day') }}</h4><AdminBarChart :labels="data.tokensSeries.map((d) => d.date.slice(5))" :series="[{ label: t('tokens'), values: data.tokensSeries.map((d) => d.value) }]" :format="(v: number) => formatNumber(v)" :height="180" /></div>
      <div class="sv__cols">
        <div class="gm-card sv__box">
          <h4>{{ t('adm_top_saving_apps') }}</h4>
          <ElTable :data="data.topApps" size="small" class="gm-table">
            <ElTableColumn :label="t('app')" min-width="200"><template #default="{ row }"><RouterLink :to="`/app/${row.slug}`" class="gm-link">{{ row.name }}</RouterLink> <ElTag v-if="row.isOverride" size="small">{{ t('cost_from_uploader') }}</ElTag></template></ElTableColumn>
            <ElTableColumn prop="downloads" :label="t('downloads')" width="100" align="right" />
            <ElTableColumn :label="t('adm_tokens_per_download')" width="140" align="right"><template #default="{ row }">{{ formatNumber(row.tokensPerDownload) }}</template></ElTableColumn>
            <ElTableColumn :label="t('adm_tokens_saved')" width="130" align="right"><template #default="{ row }">{{ formatNumber(row.tokensSaved) }}</template></ElTableColumn>
            <ElTableColumn :label="t('savings_money')" width="110" align="right"><template #default="{ row }">{{ formatMoney(row.costSavedUsd) }}</template></ElTableColumn>
          </ElTable>
        </div>
        <div class="gm-card sv__box">
          <h4>{{ t('adm_coefficients') }}</h4>
          <div v-for="(v, k) in data.coefficients" :key="k" class="sv__row"><span>{{ k }}</span><b>{{ v }}</b></div>
          <p class="sub">{{ t('adm_formula') }}</p>
        </div>
      </div>
    </template>
  </AdminPage>
</template>

<style scoped>
.sv__box { padding: 12px 14px; margin-top: 12px; }
.sv__box h4 { margin: 0 0 8px; }
.sv__cols { display: grid; grid-template-columns: 2fr 1fr; gap: 12px; }
.sv__row { display: flex; justify-content: space-between; padding: 4px 0; border-top: 1px solid var(--gm-border); font-size: 13px; }
@media (max-width: 1000px) { .sv__cols { grid-template-columns: 1fr; } }
</style>
