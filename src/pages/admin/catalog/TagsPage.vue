<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import AdminDataTable from '@/components/admin/AdminDataTable.vue'
import { AdminCatalogService } from '@/utils/services/admin-service'
import type { AdminTagDto } from '@/utils/models/admin-models'
import type { Paged } from '@/utils/models/common-models'
import { confirmX, notifyError, notifyS, promptX } from '@/utils/tools'

const { t } = useI18n()
const service = new AdminCatalogService()
const tableRef = ref<InstanceType<typeof AdminDataTable>>()
function fetchPage(query: Record<string, unknown>) {
  return service.tags(query) as unknown as Promise<Paged<Record<string, unknown>>>
}
async function rename(row: AdminTagDto) {
  const v = await promptX(t('adm_new_name'), undefined, row.name)
  if (!v) return
  await service.updateTag(row.id, v, row.isBlocked).then(() => tableRef.value?.reload()).catch(notifyError)
}
async function toggleBlock(row: AdminTagDto) {
  await service.updateTag(row.id, row.name, !row.isBlocked).then(() => { notifyS(t('saved')); tableRef.value?.reload() }).catch(notifyError)
}
async function merge(row: AdminTagDto) {
  const v = await promptX(t('adm_merge_into_id'))
  if (!v) return
  await service.mergeTag(row.id, Number(v)).then(() => tableRef.value?.reload()).catch(notifyError)
}
async function remove(row: AdminTagDto) {
  if (!(await confirmX(t('adm_confirm_delete')))) return
  await service.deleteTag(row.id).then(() => tableRef.value?.reload()).catch(notifyError)
}
</script>

<template>
  <AdminPage :title="t('adm_tags')">
    <AdminDataTable ref="tableRef" :fetch="fetchPage" :default-sort="{ prop: 'usage', order: 'descending' }">
      <ElTableColumn prop="id" label="#" width="70" />
      <ElTableColumn prop="name" :label="t('name')" min-width="200" sortable="custom" />
      <ElTableColumn prop="slug" label="Slug" min-width="200" />
      <ElTableColumn prop="usageCount" :label="t('adm_usage')" width="100" align="right" />
      <ElTableColumn :label="t('status')" width="110"><template #default="{ row }"><ElTag :type="row.isBlocked ? 'danger' : 'success'" size="small">{{ row.isBlocked ? t('blocked') : t('active') }}</ElTag></template></ElTableColumn>
      <ElTableColumn width="300" align="right"><template #default="{ row }"><ElButton size="small" @click="rename(row as AdminTagDto)">{{ t('rename') }}</ElButton><ElButton size="small" @click="merge(row as AdminTagDto)">{{ t('adm_merge') }}</ElButton><ElButton size="small" @click="toggleBlock(row as AdminTagDto)">{{ row.isBlocked ? t('unblock') : t('block') }}</ElButton><ElButton size="small" type="danger" text @click="remove(row as AdminTagDto)">{{ t('delete') }}</ElButton></template></ElTableColumn>
    </AdminDataTable>
  </AdminPage>
</template>
