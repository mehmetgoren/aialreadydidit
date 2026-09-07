<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { AppCardDto } from '@/utils/models/catalog-models'
import { MyAppsService } from '@/utils/services/my-apps-service'
import { useCommonStore } from '@/stores/common-store'
import StatusTag from '@/components/common/StatusTag.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import { assetUrl, confirmX, notifyError, notifyS } from '@/utils/tools'
import { formatDate } from '@/utils/format'

const { t } = useI18n()
const common = useCommonStore()
const service = new MyAppsService()
const apps = ref<AppCardDto[]>([])
const loading = ref(true)

async function load() {
  loading.value = true
  try {
    apps.value = await service.list()
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
}
onMounted(() => {
  common.setPageTitle(t('dash_my_apps'))
  load()
})

async function remove(app: AppCardDto) {
  if (!(await confirmX(t('confirm_delete_app', { name: app.name })))) return
  try {
    await service.remove(app.id)
    notifyS(t('deleted'))
    load()
  } catch (err) {
    notifyError(err)
  }
}
</script>

<template>
  <div>
    <div class="gm-section__head"><h2 class="gm-title">{{ t('dash_my_apps') }}</h2><RouterLink to="/upload"><ElButton type="primary"><ElIcon><Plus /></ElIcon>{{ t('upload_app') }}</ElButton></RouterLink></div>
    <ElTable v-loading="loading" :data="apps" class="gm-table" stripe>
      <ElTableColumn :label="t('app')" min-width="260">
        <template #default="{ row }">
          <div class="myapp">
            <img v-if="row.iconUrl" :src="assetUrl(row.iconUrl)" alt="" />
            <div class="myapp__letter" v-else>{{ row.name.slice(0, 1).toUpperCase() }}</div>
            <div>
              <RouterLink :to="row.status === 'published' || row.status === 'unlisted' ? `/app/${row.slug}` : `/upload/${row.id}`" class="gm-link"><strong>{{ row.name }}</strong></RouterLink>
              <div class="sub">{{ row.shortDescription }}</div>
            </div>
          </div>
        </template>
      </ElTableColumn>
      <ElTableColumn :label="t('status')" width="130"><template #default="{ row }"><StatusTag :value="row.status" /></template></ElTableColumn>
      <ElTableColumn :label="t('version')" width="90" prop="latestVersion" />
      <ElTableColumn :label="t('downloads')" width="110" prop="downloadCount" align="right" />
      <ElTableColumn :label="t('rating')" width="90" align="right"><template #default="{ row }">{{ row.ratingCount ? Math.round(row.ratingAvg) : '—' }}</template></ElTableColumn>
      <ElTableColumn :label="t('updated')" width="120"><template #default="{ row }">{{ formatDate(row.updatedAt) }}</template></ElTableColumn>
      <ElTableColumn :label="t('actions')" width="200" align="right">
        <template #default="{ row }">
          <RouterLink :to="`/upload/${row.id}`"><ElButton size="small" type="primary" plain>{{ t('edit') }}</ElButton></RouterLink>
          <RouterLink v-if="row.publishedAt" :to="`/dashboard/apps/${row.id}/stats`"><ElButton size="small">{{ t('stats') }}</ElButton></RouterLink>
          <ElButton v-if="!row.publishedAt" size="small" type="danger" text @click="remove(row as AppCardDto)">{{ t('delete') }}</ElButton>
        </template>
      </ElTableColumn>
      <template #empty><EmptyState :title="t('no_apps_yet')" :text="t('upload_first')"><RouterLink to="/upload"><ElButton type="primary">{{ t('upload_app') }}</ElButton></RouterLink></EmptyState></template>
    </ElTable>
  </div>
</template>

<style scoped>
.myapp { display: flex; gap: 10px; align-items: center; }
.myapp img, .myapp__letter { width: 36px; height: 36px; border-radius: 9px; object-fit: cover; }
.myapp__letter { display: grid; place-items: center; background: var(--gm-primary-light); color: var(--gm-primary); font-weight: 800; }
</style>
