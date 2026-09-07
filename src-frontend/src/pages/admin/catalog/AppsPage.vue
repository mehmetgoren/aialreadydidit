<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import AdminDataTable from '@/components/admin/AdminDataTable.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import { AdminAppsService } from '@/utils/services/admin-service'
import type { AdminAppRow } from '@/utils/models/admin-models'
import type { Paged } from '@/utils/models/common-models'
import { assetUrl, confirmX, notifyError, notifyS } from '@/utils/tools'
import { formatDate, formatNumber } from '@/utils/format'

const { t } = useI18n()
const service = new AdminAppsService()
const tableRef = ref<InstanceType<typeof AdminDataTable>>()
const filters = reactive({ status: '', category: '', uploader: '' })
const params = computed(() => ({ status: filters.status || null, category: filters.category || null, uploader: filters.uploader || null }))
const statuses = ['draft', 'pendingScan', 'pendingReview', 'published', 'rejected', 'unlisted', 'removed']
function fetchPage(query: Record<string, unknown>) {
  return service.list(query) as unknown as Promise<Paged<Record<string, unknown>>>
}
async function action(row: AdminAppRow, a: 'unlist' | 'restore' | 'remove' | 'feature' | 'unfeature') {
  if (a === 'remove' && !(await confirmX(t('adm_confirm_remove_app', { name: row.name })))) return
  try {
    await service.action(row.id, a)
    notifyS(t('saved'))
    tableRef.value?.reload()
  } catch (err) {
    notifyError(err)
  }
}
</script>

<template>
  <AdminPage :title="t('adm_apps')" :subtitle="t('adm_apps_subtitle')">
    <AdminDataTable ref="tableRef" :fetch="fetchPage" :params="params" :default-sort="{ prop: 'updated', order: 'descending' }">
      <template #filters>
        <ElSelect v-model="filters.status" :placeholder="t('status')" clearable class="f-sm"><ElOption v-for="s in statuses" :key="s" :value="s" :label="t(`status_${s}`)" /></ElSelect>
        <ElInput v-model="filters.uploader" :placeholder="t('uploader')" clearable class="f-sm" />
      </template>
      <ElTableColumn prop="name" :label="t('app')" min-width="240" sortable="custom">
        <template #default="{ row }">
          <div class="a-row"><img v-if="row.iconUrl" :src="assetUrl(row.iconUrl)" alt="" /><div><RouterLink :to="`/admin/apps/${row.id}`" class="gm-link"><strong>{{ row.name }}</strong></RouterLink> <ElTag v-if="row.isFeatured" type="warning" size="small">★</ElTag><div class="sub">{{ row.slug }} · {{ row.categoryNameEn }}</div></div></div>
        </template>
      </ElTableColumn>
      <ElTableColumn :label="t('status')" width="120"><template #default="{ row }"><StatusTag :value="row.status" /></template></ElTableColumn>
      <ElTableColumn :label="t('uploader')" width="140"><template #default="{ row }"><RouterLink to="/admin/users" class="gm-link">{{ row.uploaderUsername }}</RouterLink></template></ElTableColumn>
      <ElTableColumn :label="t('license')" prop="licenseSpdxId" width="110" />
      <ElTableColumn :label="t('generated_by')" prop="llmModelName" width="150" show-overflow-tooltip />
      <ElTableColumn prop="downloads" :label="t('downloads')" width="100" align="right" sortable="custom"><template #default="{ row }">{{ formatNumber(row.downloadCount) }}</template></ElTableColumn>
      <ElTableColumn prop="rating" :label="t('rating')" width="90" align="right" sortable="custom"><template #default="{ row }">{{ row.ratingCount ? `${Math.round(row.ratingAvg)} (${row.ratingCount})` : '—' }}</template></ElTableColumn>
      <ElTableColumn :label="t('adm_embedding')" width="90" align="center"><template #default="{ row }"><ElTag :type="row.hasEmbedding && !row.embeddingStale ? 'success' : 'warning'" size="small">{{ row.hasEmbedding ? (row.embeddingStale ? t('adm_stale') : 'ok') : '—' }}</ElTag></template></ElTableColumn>
      <ElTableColumn :label="t('adm_reports')" width="80" align="center"><template #default="{ row }"><ElTag v-if="row.openReports" type="danger" size="small">{{ row.openReports }}</ElTag><span v-else class="gm-muted">0</span></template></ElTableColumn>
      <ElTableColumn prop="updated" :label="t('updated')" width="110" sortable="custom"><template #default="{ row }">{{ formatDate(row.updatedAt) }}</template></ElTableColumn>
      <ElTableColumn :label="t('actions')" width="200" align="right" fixed="right">
        <template #default="{ row }">
          <ElDropdown trigger="click" @command="action(row as AdminAppRow, $event)">
            <ElButton size="small">{{ t('actions') }} <ElIcon><ArrowDown /></ElIcon></ElButton>
            <template #dropdown>
              <ElDropdownMenu>
                <ElDropdownItem v-if="row.status === 'published'" command="unlist">{{ t('unlist') }}</ElDropdownItem>
                <ElDropdownItem v-if="row.status === 'unlisted' || row.status === 'removed'" command="restore">{{ t('adm_restore') }}</ElDropdownItem>
                <ElDropdownItem :command="row.isFeatured ? 'unfeature' : 'feature'">{{ row.isFeatured ? t('adm_unfeature') : t('adm_feature') }}</ElDropdownItem>
                <ElDropdownItem command="remove" divided>{{ t('adm_remove') }}</ElDropdownItem>
              </ElDropdownMenu>
            </template>
          </ElDropdown>
          <ElButton size="small" type="primary" @click="$router.push(`/admin/apps/${row.id}`)">{{ t('edit') }}</ElButton>
        </template>
      </ElTableColumn>
    </AdminDataTable>
  </AdminPage>
</template>

<style scoped>
.a-row { display: flex; gap: 8px; align-items: center; }
.a-row img { width: 32px; height: 32px; border-radius: 8px; }
</style>
