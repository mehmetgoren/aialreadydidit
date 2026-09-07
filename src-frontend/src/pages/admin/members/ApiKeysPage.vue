<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import AdminDataTable from '@/components/admin/AdminDataTable.vue'
import { AdminIdentityService } from '@/utils/services/admin-service'
import type { AdminApiKeyRow } from '@/utils/models/admin-models'
import type { Paged } from '@/utils/models/common-models'
import { confirmX, notifyError, notifyS } from '@/utils/tools'
import { formatDateTime, formatNumber } from '@/utils/format'

const { t } = useI18n()
const service = new AdminIdentityService()
const tableRef = ref<InstanceType<typeof AdminDataTable>>()
const status = ref('active')
const params = computed(() => ({ status: status.value }))
function fetchPage(query: Record<string, unknown>) {
  return service.apiKeys(query) as unknown as Promise<Paged<Record<string, unknown>>>
}
async function revoke(row: AdminApiKeyRow) {
  if (!(await confirmX(t('confirm_revoke_key', { name: row.name })))) return
  await service.revokeApiKey(row.id).then(() => tableRef.value?.reload()).catch(notifyError)
}
async function tier(row: AdminApiKeyRow, rateTier: string) {
  await service.setTier(row.id, rateTier).then(() => { notifyS(t('saved')); tableRef.value?.reload() }).catch(notifyError)
}
</script>

<template>
  <AdminPage :title="t('adm_api_keys')" :subtitle="t('adm_api_keys_subtitle')">
    <AdminDataTable ref="tableRef" :fetch="fetchPage" :params="params">
      <template #filters><ElRadioGroup v-model="status" size="small"><ElRadioButton value="active">{{ t('active') }}</ElRadioButton><ElRadioButton value="all">{{ t('all') }}</ElRadioButton></ElRadioGroup></template>
      <ElTableColumn :label="t('name')" prop="name" min-width="150" />
      <ElTableColumn :label="t('key')" prop="prefix" width="130" />
      <ElTableColumn :label="t('username')" width="160"><template #default="{ row }"><RouterLink :to="`/admin/users/${row.userId}`" class="gm-link">{{ row.username }}</RouterLink><div class="sub">{{ row.email }}</div></template></ElTableColumn>
      <ElTableColumn :label="t('scopes')" prop="scopes" width="160" />
      <ElTableColumn :label="t('adm_tier')" width="150"><template #default="{ row }"><ElSelect :model-value="row.rateTier" size="small" @update:model-value="tier(row as AdminApiKeyRow, $event)"><ElOption value="default" label="default" /><ElOption value="elevated" label="elevated" /><ElOption value="unlimited" label="unlimited" /></ElSelect></template></ElTableColumn>
      <ElTableColumn :label="t('requests')" width="100" align="right"><template #default="{ row }">{{ formatNumber(row.requestCount) }}</template></ElTableColumn>
      <ElTableColumn :label="t('downloads')" prop="downloadCount" width="100" align="right" />
      <ElTableColumn :label="t('last_used')" width="150"><template #default="{ row }">{{ row.lastUsedAt ? formatDateTime(row.lastUsedAt) : '—' }}</template></ElTableColumn>
      <ElTableColumn :label="t('status')" width="100"><template #default="{ row }"><ElTag :type="row.revokedAt ? 'info' : 'success'" size="small">{{ row.revokedAt ? t('revoked') : t('active') }}</ElTag></template></ElTableColumn>
      <ElTableColumn width="100" align="right"><template #default="{ row }"><ElButton v-if="!row.revokedAt" size="small" type="danger" text @click="revoke(row as AdminApiKeyRow)">{{ t('revoke') }}</ElButton></template></ElTableColumn>
    </AdminDataTable>
  </AdminPage>
</template>
