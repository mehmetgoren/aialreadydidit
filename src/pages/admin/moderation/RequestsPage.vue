<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import AdminDataTable from '@/components/admin/AdminDataTable.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import { AdminRequestsService } from '@/utils/services/admin-service'
import type { AdminRequestDto } from '@/utils/models/admin-models'
import type { Paged } from '@/utils/models/common-models'
import { confirmX, notifyError, notifyS, promptX } from '@/utils/tools'
import { formatDateTime } from '@/utils/format'

const { t } = useI18n()
const service = new AdminRequestsService()
const tableRef = ref<InstanceType<typeof AdminDataTable>>()
const status = ref('open')
const params = computed(() => ({ status: status.value, sort: 'votes' }))
function fetchPage(query: Record<string, unknown>) {
  return service.list(query) as unknown as Promise<Paged<Record<string, unknown>>>
}
async function setStatus(r: AdminRequestDto, s: string) {
  try {
    let appId: number | null = null
    if (s === 'fulfilled') {
      const v = await promptX(t('adm_fulfil_app_id'))
      if (!v) return
      appId = Number(v)
    }
    await service.setStatus(r.id, s, appId)
    notifyS(t('saved'))
    tableRef.value?.reload()
  } catch (err) {
    notifyError(err)
  }
}
async function remove(r: AdminRequestDto) {
  if (!(await confirmX(t('adm_confirm_delete')))) return
  await service.remove(r.id).catch(notifyError)
  tableRef.value?.reload()
}
</script>

<template>
  <AdminPage :title="t('adm_requests')" :subtitle="t('adm_requests_subtitle')">
    <AdminDataTable ref="tableRef" :fetch="fetchPage" :params="params">
      <template #filters>
        <ElRadioGroup v-model="status" size="small"><ElRadioButton value="open">{{ t('status_open') }}</ElRadioButton><ElRadioButton value="fulfilled">{{ t('status_fulfilled') }}</ElRadioButton><ElRadioButton value="closed">{{ t('status_closed') }}</ElRadioButton><ElRadioButton value="">{{ t('all') }}</ElRadioButton></ElRadioGroup>
      </template>
      <ElTableColumn :label="t('title')" min-width="240"><template #default="{ row }"><RouterLink :to="`/wanted/${row.id}`" target="_blank" class="gm-link"><strong>{{ row.title }}</strong></RouterLink><div class="sub">{{ row.description }}</div></template></ElTableColumn>
      <ElTableColumn :label="t('votes')" prop="voteCount" width="80" align="center" />
      <ElTableColumn :label="t('adm_source')" prop="source" width="90" />
      <ElTableColumn :label="t('adm_requester')" width="140"><template #default="{ row }">{{ row.requesterUsername ?? t('an_agent') }}</template></ElTableColumn>
      <ElTableColumn :label="t('status')" width="120"><template #default="{ row }"><StatusTag :value="row.status" /><RouterLink v-if="row.fulfilledByAppSlug" :to="`/app/${row.fulfilledByAppSlug}`" class="gm-link" target="_blank"> {{ row.fulfilledByAppName }}</RouterLink></template></ElTableColumn>
      <ElTableColumn :label="t('date')" width="150"><template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template></ElTableColumn>
      <ElTableColumn width="260" align="right" fixed="right">
        <template #default="{ row }">
          <ElButton size="small" type="success" plain @click="setStatus(row as AdminRequestDto, 'fulfilled')">{{ t('status_fulfilled') }}</ElButton>
          <ElButton size="small" @click="setStatus(row as AdminRequestDto, row.status === 'closed' ? 'open' : 'closed')">{{ row.status === 'closed' ? t('reopen') : t('close') }}</ElButton>
          <ElButton size="small" type="danger" text @click="remove(row as AdminRequestDto)">{{ t('delete') }}</ElButton>
        </template>
      </ElTableColumn>
    </AdminDataTable>
  </AdminPage>
</template>
