<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import AdminBarChart from '@/components/admin/AdminBarChart.vue'
import { AdminSystemService } from '@/utils/services/admin-service'
import type { StatsOverview } from '@/utils/models/admin-models'
import { notifyError } from '@/utils/tools'
import { formatNumber } from '@/utils/format'

const { t } = useI18n()
const service = new AdminSystemService()
const days = ref(30)
const data = ref<StatsOverview | null>(null)
const loading = ref(true)
async function load() { loading.value = true; data.value = await service.stats(days.value).catch((e) => { notifyError(e); return null }); loading.value = false }
onMounted(load)
watch(days, load)
</script>

<template>
  <AdminPage :title="t('adm_stats_overview')" :loading="loading">
    <template #actions><ElRadioGroup v-model="days" size="small"><ElRadioButton :value="7">7d</ElRadioButton><ElRadioButton :value="30">30d</ElRadioButton><ElRadioButton :value="90">90d</ElRadioButton><ElRadioButton :value="365">1y</ElRadioButton></ElRadioGroup></template>
    <template v-if="data">
      <div class="st__grid">
        <div class="gm-card st__box"><h4>{{ t('downloads') }}</h4><AdminBarChart :labels="data.downloads.map((d) => d.date.slice(5))" :series="[{ label: t('downloads'), values: data.downloads.map((d) => d.value) }]" /></div>
        <div class="gm-card st__box"><h4>{{ t('adm_searches') }} / {{ t('adm_signups') }} / {{ t('adm_uploads') }}</h4><AdminBarChart type="line" :labels="data.searches.map((d) => d.date.slice(5))" :series="[{ label: t('adm_searches'), values: data.searches.map((d) => d.value) }, { label: t('adm_signups'), values: data.signups.map((d) => d.value) }, { label: t('adm_uploads'), values: data.uploads.map((d) => d.value) }]" /></div>
      </div>
      <div class="st__facets">
        <div v-for="(list, key) in { by_platform: data.downloadsByPlatform, by_source: data.downloadsBySource, adm_apps_by_category: data.appsByCategory, adm_apps_by_model: data.appsByModel, adm_apps_by_license: data.appsByLicense }" :key="key" class="gm-card st__box">
          <h4>{{ t(key) }}</h4>
          <div v-for="f in list" :key="f.value" class="st__row"><span>{{ f.label }}</span><b>{{ formatNumber(f.count) }}</b></div>
          <div v-if="!list.length" class="gm-muted">—</div>
        </div>
        <div class="gm-card st__box"><h4>{{ t('adm_top_apps') }}</h4><div v-for="a in data.topApps" :key="a.id" class="st__row"><RouterLink :to="`/app/${a.slug}`" class="gm-link">{{ a.name }}</RouterLink><b>{{ formatNumber(a.downloadCount) }}</b></div></div>
        <div class="gm-card st__box"><h4>{{ t('adm_top_uploaders') }}</h4><div v-for="u in data.topUploaders" :key="u.username" class="st__row"><RouterLink :to="`/u/${u.username}`" class="gm-link">{{ u.displayName }}</RouterLink><b>{{ u.apps }} / {{ formatNumber(u.downloads) }}</b></div></div>
      </div>
    </template>
  </AdminPage>
</template>

<style scoped>
.st__grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.st__facets { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 12px; margin-top: 12px; }
.st__box { padding: 12px 14px; }
.st__box h4 { margin: 0 0 8px; }
.st__row { display: flex; justify-content: space-between; padding: 4px 0; border-top: 1px solid var(--gm-border); font-size: 13px; }
@media (max-width: 1000px) { .st__grid { grid-template-columns: 1fr; } }
</style>
