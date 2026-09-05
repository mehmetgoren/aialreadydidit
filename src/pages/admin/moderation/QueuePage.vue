<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import AdminDataTable from '@/components/admin/AdminDataTable.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import { AdminModerationService } from '@/utils/services/admin-service'
import type { Paged } from '@/utils/models/common-models'
import { formatDateTime } from '@/utils/format'
import { assetUrl } from '@/utils/tools'

const { t } = useI18n()
const route = useRoute()
const service = new AdminModerationService()
const status = ref(typeof route.query.status === 'string' ? route.query.status : 'review')
const params = computed(() => ({ status: status.value }))
function fetchPage(query: Record<string, unknown>) {
  return service.queue(query) as unknown as Promise<Paged<Record<string, unknown>>>
}
</script>

<template>
  <AdminPage :title="t('adm_moderation_queue')" :subtitle="t('adm_moderation_subtitle')">
    <AdminDataTable :fetch="fetchPage" :params="params" :search-placeholder="t('adm_search_app_or_member')">
      <template #filters>
        <ElRadioGroup v-model="status" size="small">
          <ElRadioButton value="review">{{ t('adm_pending_review') }}</ElRadioButton>
          <ElRadioButton value="scan">{{ t('adm_pending_scan') }}</ElRadioButton>
          <ElRadioButton value="rejected">{{ t('status_rejected') }}</ElRadioButton>
          <ElRadioButton value="all">{{ t('all') }}</ElRadioButton>
        </ElRadioGroup>
      </template>
      <ElTableColumn :label="t('app')" min-width="260">
        <template #default="{ row }">
          <div class="q-app">
            <img v-if="row.coverUrl" :src="assetUrl(row.coverUrl)" alt="" />
            <div>
              <RouterLink :to="`/admin/moderation/${row.appId}`" class="gm-link"><strong>{{ row.name }}</strong></RouterLink>
              <div class="sub">{{ row.shortDescription }}</div>
            </div>
          </div>
        </template>
      </ElTableColumn>
      <ElTableColumn :label="t('adm_kind')" width="110"><template #default="{ row }">{{ row.kind === 'version' ? `v${row.version}` : t('adm_new_app') }}</template></ElTableColumn>
      <ElTableColumn :label="t('status')" width="130"><template #default="{ row }"><StatusTag :value="row.versionStatus ?? row.appStatus" /></template></ElTableColumn>
      <ElTableColumn :label="t('uploader')" width="150"><template #default="{ row }">{{ row.uploaderUsername }} <ElTag v-if="row.uploaderTrustLevel" size="small" type="success">T{{ row.uploaderTrustLevel }}</ElTag></template></ElTableColumn>
      <ElTableColumn :label="t('category')" prop="categoryName" width="160" show-overflow-tooltip />
      <ElTableColumn :label="t('license')" width="130"><template #default="{ row }"><ElTag :type="row.licenseOk ? 'success' : 'danger'" size="small">{{ row.licenseSpdxId }}</ElTag></template></ElTableColumn>
      <ElTableColumn :label="t('files')" width="120"><template #default="{ row }">{{ row.fileCount }} <ElTag v-if="row.infectedCount" type="danger" size="small">{{ row.infectedCount }} ⚠</ElTag><ElTag v-else-if="row.pendingScanCount" type="warning" size="small">{{ row.pendingScanCount }} ⏳</ElTag></template></ElTableColumn>
      <ElTableColumn :label="t('screenshots')" prop="screenshotCount" width="90" align="center" />
      <ElTableColumn :label="t('adm_reports')" width="80" align="center"><template #default="{ row }"><ElTag v-if="row.openReports" type="danger" size="small">{{ row.openReports }}</ElTag><span v-else class="gm-muted">0</span></template></ElTableColumn>
      <ElTableColumn :label="t('submitted')" width="150"><template #default="{ row }">{{ formatDateTime(row.submittedAt) }}</template></ElTableColumn>
      <ElTableColumn width="100" align="right" fixed="right"><template #default="{ row }"><ElButton size="small" type="primary" @click="$router.push(`/admin/moderation/${row.appId}`)">{{ t('adm_review') }}</ElButton></template></ElTableColumn>
    </AdminDataTable>
  </AdminPage>
</template>

<style scoped>
.q-app { display: flex; gap: 10px; align-items: center; }
.q-app img { width: 64px; height: 40px; object-fit: cover; border-radius: 6px; }
</style>
