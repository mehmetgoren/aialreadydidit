<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import AdminDataTable from '@/components/admin/AdminDataTable.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import { AdminReportsService } from '@/utils/services/admin-service'
import type { AdminReportDto } from '@/utils/models/admin-models'
import type { Paged } from '@/utils/models/common-models'
import { enableAfter, notifyError, notifyS } from '@/utils/tools'
import { formatDateTime } from '@/utils/format'

const { t } = useI18n()
const service = new AdminReportsService()
const tableRef = ref<InstanceType<typeof AdminDataTable>>()
const status = ref('open')
const params = computed(() => ({ status: status.value }))
const dialog = ref(false)
const saving = ref(false)
const current = ref<AdminReportDto | null>(null)
const form = reactive({ status: 'resolved' as AdminReportDto['status'], resolution: '', action: 'none' as 'none' | 'unlist' | 'remove' | 'hide_review' | 'ban_uploader' })
function fetchPage(query: Record<string, unknown>) {
  return service.list(query) as unknown as Promise<Paged<Record<string, unknown>>>
}
function open(r: AdminReportDto) {
  current.value = r
  Object.assign(form, { status: 'resolved', resolution: '', action: 'none' })
  dialog.value = true
}
async function save() {
  if (!current.value) return
  await enableAfter(saving, async () => {
    try {
      await service.resolve(current.value!.id, { status: form.status, resolution: form.resolution || null, action: form.action })
      notifyS(t('saved'))
      dialog.value = false
      tableRef.value?.reload()
    } catch (err) {
      notifyError(err)
    }
  })
}
</script>

<template>
  <AdminPage :title="t('adm_reports')" :subtitle="t('adm_reports_subtitle')">
    <AdminDataTable ref="tableRef" :fetch="fetchPage" :params="params">
      <template #filters>
        <ElRadioGroup v-model="status" size="small">
          <ElRadioButton value="open">{{ t('status_open') }}</ElRadioButton>
          <ElRadioButton value="resolved">{{ t('status_resolved') }}</ElRadioButton>
          <ElRadioButton value="dismissed">{{ t('status_dismissed') }}</ElRadioButton>
          <ElRadioButton value="all">{{ t('all') }}</ElRadioButton>
        </ElRadioGroup>
      </template>
      <ElTableColumn :label="t('app')" min-width="200"><template #default="{ row }"><RouterLink v-if="row.appSlug" :to="`/app/${row.appSlug}`" target="_blank" class="gm-link">{{ row.appName }}</RouterLink><span v-else>—</span> <StatusTag v-if="row.appStatus" :value="row.appStatus" /></template></ElTableColumn>
      <ElTableColumn :label="t('report_reason')" width="150"><template #default="{ row }">{{ t(`report_reason_${row.reason}`) }}</template></ElTableColumn>
      <ElTableColumn :label="t('report_details')" prop="details" min-width="240" show-overflow-tooltip />
      <ElTableColumn :label="t('adm_review_col')" width="160" show-overflow-tooltip><template #default="{ row }">{{ row.ratingReview ?? '—' }}</template></ElTableColumn>
      <ElTableColumn :label="t('adm_reporter')" width="140"><template #default="{ row }">{{ row.reporterUsername ?? row.reporterEmail ?? t('anonymous') }}</template></ElTableColumn>
      <ElTableColumn :label="t('status')" width="110"><template #default="{ row }"><StatusTag :value="row.status" /></template></ElTableColumn>
      <ElTableColumn :label="t('date')" width="150"><template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template></ElTableColumn>
      <ElTableColumn width="120" align="right" fixed="right"><template #default="{ row }"><ElButton size="small" type="primary" @click="open(row as AdminReportDto)">{{ t('adm_resolve') }}</ElButton></template></ElTableColumn>
    </AdminDataTable>
    <ElDialog v-model="dialog" :title="`${t('adm_resolve')} #${current?.id}`" width="520px">
      <p v-if="current"><strong>{{ current.appName }}</strong> · {{ t(`report_reason_${current.reason}`) }}<br /><span class="gm-muted">{{ current.details }}</span></p>
      <ElForm label-position="top">
        <ElFormItem :label="t('status')"><ElRadioGroup v-model="form.status"><ElRadio value="resolved">{{ t('status_resolved') }}</ElRadio><ElRadio value="dismissed">{{ t('status_dismissed') }}</ElRadio><ElRadio value="reviewing">{{ t('status_reviewing') }}</ElRadio></ElRadioGroup></ElFormItem>
        <ElFormItem :label="t('adm_action')">
          <ElSelect v-model="form.action" style="width: 100%">
            <ElOption value="none" :label="t('adm_action_none')" /><ElOption value="unlist" :label="t('adm_action_unlist')" /><ElOption value="remove" :label="t('adm_action_remove')" />
            <ElOption v-if="current?.ratingId" value="hide_review" :label="t('adm_action_hide_review')" /><ElOption value="ban_uploader" :label="t('adm_action_ban')" />
          </ElSelect>
        </ElFormItem>
        <ElFormItem :label="t('adm_resolution')"><ElInput v-model="form.resolution" type="textarea" :rows="3" /></ElFormItem>
      </ElForm>
      <template #footer><ElButton @click="dialog = false">{{ t('cancel') }}</ElButton><ElButton type="primary" :loading="saving" @click="save">{{ t('save') }}</ElButton></template>
    </ElDialog>
  </AdminPage>
</template>
