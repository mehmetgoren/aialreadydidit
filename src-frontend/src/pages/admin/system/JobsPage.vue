<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import AdminDataTable from '@/components/admin/AdminDataTable.vue'
import AdminJsonView from '@/components/admin/AdminJsonView.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import { AdminSystemService } from '@/utils/services/admin-service'
import type { JobDto } from '@/utils/models/admin-models'
import type { Paged } from '@/utils/models/common-models'
import { confirmX, notifyError, notifyS } from '@/utils/tools'
import { formatDateTime } from '@/utils/format'

const { t } = useI18n()
const service = new AdminSystemService()
const tableRef = ref<InstanceType<typeof AdminDataTable>>()
const status = ref('')
const params = computed(() => ({ status: status.value || null }))
const detail = ref<JobDto | null>(null)
function fetchPage(query: Record<string, unknown>) {
  return service.jobs(query) as unknown as Promise<Paged<Record<string, unknown>>>
}
async function retry(j: JobDto) { await service.retryJob(j.id).then(() => tableRef.value?.reload()).catch(notifyError) }
async function cancel(j: JobDto) { await service.cancelJob(j.id).then(() => tableRef.value?.reload()).catch(notifyError) }
async function maintenance(task: string) {
  if (!(await confirmX(t('adm_confirm_maintenance', { task })))) return
  await service.maintenance(task).then(() => { notifyS(t('adm_queued')); tableRef.value?.reload() }).catch(notifyError)
}
</script>

<template>
  <AdminPage :title="t('adm_system_jobs')" :subtitle="t('adm_jobs_subtitle')">
    <template #actions>
      <ElDropdown trigger="click" @command="maintenance">
        <ElButton>{{ t('adm_maintenance') }} <ElIcon><ArrowDown /></ElIcon></ElButton>
        <template #dropdown><ElDropdownMenu><ElDropdownItem command="reembed_all">{{ t('adm_reembed_all') }}</ElDropdownItem><ElDropdownItem command="recompute_stats">{{ t('adm_recompute_stats') }}</ElDropdownItem><ElDropdownItem command="recompute_estimates">{{ t('adm_recompute_estimates') }}</ElDropdownItem><ElDropdownItem command="rescan_pending">{{ t('adm_rescan_pending') }}</ElDropdownItem><ElDropdownItem command="purge_done_jobs">{{ t('adm_purge_done') }}</ElDropdownItem></ElDropdownMenu></template>
      </ElDropdown>
    </template>
    <AdminDataTable ref="tableRef" :fetch="fetchPage" :params="params" :search-placeholder="t('adm_job_type_or_subject')">
      <template #filters><ElSelect v-model="status" :placeholder="t('status')" clearable class="f-sm"><ElOption v-for="s in ['queued', 'running', 'done', 'failed', 'cancelled']" :key="s" :value="s" :label="t(`status_${s}`)" /></ElSelect></template>
      <ElTableColumn prop="id" label="#" width="80" />
      <ElTableColumn prop="type" :label="t('adm_kind')" width="170" />
      <ElTableColumn prop="subject" :label="t('adm_subject')" width="140" />
      <ElTableColumn :label="t('status')" width="110"><template #default="{ row }"><StatusTag :value="row.status" /></template></ElTableColumn>
      <ElTableColumn :label="t('adm_attempts')" width="90" align="center"><template #default="{ row }">{{ row.attempts }}/{{ row.maxAttempts }}</template></ElTableColumn>
      <ElTableColumn :label="t('adm_run_at')" width="160"><template #default="{ row }">{{ formatDateTime(row.runAt) }}</template></ElTableColumn>
      <ElTableColumn :label="t('adm_finished')" width="160"><template #default="{ row }">{{ row.finishedAt ? formatDateTime(row.finishedAt) : '—' }}</template></ElTableColumn>
      <ElTableColumn prop="lastError" :label="t('adm_error')" min-width="220" show-overflow-tooltip />
      <ElTableColumn width="200" align="right" fixed="right"><template #default="{ row }"><ElButton size="small" text @click="detail = row as JobDto">{{ t('detail') }}</ElButton><ElButton v-if="row.status === 'failed' || row.status === 'cancelled'" size="small" @click="retry(row as JobDto)">{{ t('adm_retry') }}</ElButton><ElButton v-if="row.status === 'queued'" size="small" type="danger" text @click="cancel(row as JobDto)">{{ t('cancel') }}</ElButton></template></ElTableColumn>
    </AdminDataTable>
    <ElDialog :model-value="Boolean(detail)" :title="`#${detail?.id} ${detail?.type}`" width="700px" @close="detail = null"><AdminJsonView :value="detail?.payloadJson" /><pre v-if="detail?.lastError" class="err">{{ detail.lastError }}</pre></ElDialog>
  </AdminPage>
</template>

<style scoped>
.err { white-space: pre-wrap; font-size: 12px; color: var(--el-color-danger); max-height: 320px; overflow: auto; }
</style>
