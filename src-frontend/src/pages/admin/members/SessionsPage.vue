<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import AdminDataTable from '@/components/admin/AdminDataTable.vue'
import { AdminIdentityService } from '@/utils/services/admin-service'
import type { AdminSessionDto } from '@/utils/models/admin-models'
import type { Paged } from '@/utils/models/common-models'
import { notifyError } from '@/utils/tools'
import { formatDateTime } from '@/utils/format'

const { t } = useI18n()
const service = new AdminIdentityService()
const tableRef = ref<InstanceType<typeof AdminDataTable>>()
const status = ref('active')
const params = computed(() => ({ status: status.value }))
function fetchPage(query: Record<string, unknown>) {
  return service.sessions(query) as unknown as Promise<Paged<Record<string, unknown>>>
}
async function revoke(row: AdminSessionDto) {
  await service.revokeSession(row.id).then(() => tableRef.value?.reload()).catch(notifyError)
}
</script>

<template>
  <AdminPage :title="t('adm_identity_sessions')" :subtitle="t('adm_sessions_subtitle')">
    <AdminDataTable ref="tableRef" :fetch="fetchPage" :params="params">
      <template #filters><ElRadioGroup v-model="status" size="small"><ElRadioButton value="active">{{ t('active') }}</ElRadioButton><ElRadioButton value="all">{{ t('all') }}</ElRadioButton></ElRadioGroup></template>
      <ElTableColumn :label="t('username')" width="160"><template #default="{ row }"><RouterLink :to="`/admin/users/${row.userId}`" class="gm-link">{{ row.username }}</RouterLink></template></ElTableColumn>
      <ElTableColumn :label="t('created')" width="150"><template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template></ElTableColumn>
      <ElTableColumn :label="t('last_used')" width="150"><template #default="{ row }">{{ row.lastUsedAt ? formatDateTime(row.lastUsedAt) : '—' }}</template></ElTableColumn>
      <ElTableColumn :label="t('expires')" width="150"><template #default="{ row }">{{ formatDateTime(row.expiresAt) }}</template></ElTableColumn>
      <ElTableColumn prop="ip" label="IP" width="140" />
      <ElTableColumn prop="userAgent" :label="t('device')" min-width="240" show-overflow-tooltip />
      <ElTableColumn width="100" align="right"><template #default="{ row }"><ElButton v-if="!row.revoked" size="small" type="danger" text @click="revoke(row as AdminSessionDto)">{{ t('revoke') }}</ElButton><ElTag v-else size="small" type="info">{{ t('revoked') }}</ElTag></template></ElTableColumn>
    </AdminDataTable>
  </AdminPage>
</template>
