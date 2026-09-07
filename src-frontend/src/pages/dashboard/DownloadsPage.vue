<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { DownloadHistoryDto } from '@/utils/models/dashboard-models'
import { DashboardService } from '@/utils/services/dashboard-service'
import { useCommonStore } from '@/stores/common-store'
import PagePagination from '@/components/common/PagePagination.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import { assetUrl, notifyError, platformIcon } from '@/utils/tools'
import { formatDateTime } from '@/utils/format'

const { t } = useI18n()
const common = useCommonStore()
const rows = ref<DownloadHistoryDto[]>([])
const total = ref(0)
const page = ref(1)
const loading = ref(true)

async function load() {
  loading.value = true
  try {
    const p = await new DashboardService().downloads(page.value)
    rows.value = p.items
    total.value = p.totalCount
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
}
onMounted(() => {
  common.setPageTitle(t('dash_downloads'))
  load()
})
watch(page, load)
</script>

<template>
  <div>
    <h2 class="gm-title">{{ t('dash_downloads') }}</h2>
    <div v-loading="loading" class="dl-list">
      <EmptyState v-if="!rows.length && !loading" :title="t('no_downloads')" icon="Download" />
      <div v-for="d in rows" :key="d.id" class="dl-row">
        <img v-if="d.app.iconUrl" :src="assetUrl(d.app.iconUrl)" alt="" />
        <div class="dl-row__letter" v-else>{{ d.app.name.slice(0, 1) }}</div>
        <div class="dl-row__body">
          <RouterLink :to="`/app/${d.app.slug}`" class="gm-link"><strong>{{ d.app.name }}</strong></RouterLink>
          <div class="sub">{{ platformIcon(d.platformCode) }} {{ d.fileName }} · v{{ d.version }} · {{ formatDateTime(d.createdAt) }}</div>
        </div>
        <RouterLink v-if="!d.rated" :to="`/app/${d.app.slug}#reviews`"><ElButton size="small" type="primary" plain>{{ t('rate_this_app') }}</ElButton></RouterLink>
        <ElTag v-else type="success" size="small">{{ t('rated') }}</ElTag>
      </div>
    </div>
    <PagePagination v-model:page="page" :page-size="20" :total="total" />
  </div>
</template>

<style scoped>
.dl-list { display: flex; flex-direction: column; gap: 8px; min-height: 80px; }
.dl-row { display: flex; align-items: center; gap: 12px; padding: 10px 12px; border: 1px solid var(--gm-border); border-radius: 10px; }
.dl-row img, .dl-row__letter { width: 36px; height: 36px; border-radius: 9px; object-fit: cover; }
.dl-row__letter { display: grid; place-items: center; background: var(--gm-primary-light); color: var(--gm-primary); font-weight: 800; }
.dl-row__body { flex: 1; min-width: 0; }
</style>
