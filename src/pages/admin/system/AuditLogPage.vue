<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import AdminDataTable from '@/components/admin/AdminDataTable.vue'
import AdminJsonView from '@/components/admin/AdminJsonView.vue'
import { AdminSystemService } from '@/utils/services/admin-service'
import type { AuditLogDto } from '@/utils/models/admin-models'
import type { Paged } from '@/utils/models/common-models'
import { formatDateTime } from '@/utils/format'

const { t } = useI18n()
const service = new AdminSystemService()
const filters = reactive({ range: null as [string, string] | null })
const params = computed(() => ({ from: filters.range?.[0] ?? null, to: filters.range?.[1] ?? null }))
const detail = ref<AuditLogDto | null>(null)
function fetchPage(query: Record<string, unknown>) {
  return service.audit(query) as unknown as Promise<Paged<Record<string, unknown>>>
}
</script>

<template>
  <AdminPage :title="t('adm_identity_audit_log')" :subtitle="t('adm_audit_subtitle')">
    <AdminDataTable :fetch="fetchPage" :params="params" :search-placeholder="t('adm_audit_search')">
      <template #filters><ElDatePicker v-model="filters.range" type="daterange" value-format="YYYY-MM-DD" class="f-range" /></template>
      <ElTableColumn :label="t('date')" width="160"><template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template></ElTableColumn>
      <ElTableColumn prop="username" :label="t('username')" width="140" />
      <ElTableColumn prop="action" :label="t('adm_action')" width="200" />
      <ElTableColumn :label="t('adm_entity')" width="180"><template #default="{ row }">{{ row.entity }} <code v-if="row.entityId">#{{ row.entityId }}</code></template></ElTableColumn>
      <ElTableColumn prop="details" :label="t('adm_details')" min-width="240" show-overflow-tooltip />
      <ElTableColumn prop="ipAddress" label="IP" width="130" />
      <ElTableColumn width="90" align="right"><template #default="{ row }"><ElButton size="small" text @click="detail = row as AuditLogDto">{{ t('detail') }}</ElButton></template></ElTableColumn>
    </AdminDataTable>
    <ElDialog :model-value="Boolean(detail)" :title="detail?.action" width="640px" @close="detail = null"><AdminJsonView :value="detail?.details" /></ElDialog>
  </AdminPage>
</template>
