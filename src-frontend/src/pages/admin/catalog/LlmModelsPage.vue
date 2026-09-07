<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import { AdminCatalogService } from '@/utils/services/admin-service'
import type { AdminLlmModelDto } from '@/utils/models/admin-models'
import { confirmX, enableAfter, notifyError, notifyS } from '@/utils/tools'

const { t } = useI18n()
const service = new AdminCatalogService()
const rows = ref<AdminLlmModelDto[]>([])
const loading = ref(true)
const dialog = ref(false)
const saving = ref(false)
const form = reactive<Partial<AdminLlmModelDto>>({})

async function load() {
  loading.value = true
  rows.value = await service.llmModels().catch((e) => { notifyError(e); return [] })
  loading.value = false
}
onMounted(load)
function open(row?: AdminLlmModelDto) {
  Object.assign(form, row ? { ...row } : { id: undefined, vendor: '', name: '', version: '', releasedOn: null, isActive: true, sortOrder: rows.value.length + 1 })
  dialog.value = true
}
async function save() {
  await enableAfter(saving, async () => {
    try {
      await service.saveLlmModel(form.id ?? null, form)
      notifyS(t('saved'))
      dialog.value = false
      load()
    } catch (err) {
      notifyError(err)
    }
  })
}
async function remove(row: AdminLlmModelDto) {
  if (!(await confirmX(t('adm_confirm_delete')))) return
  await service.deleteLlmModel(row.id).then(load).catch(notifyError)
}
</script>

<template>
  <AdminPage :title="t('adm_llm_models')" :subtitle="t('adm_llm_models_subtitle')" :loading="loading">
    <template #actions><ElButton type="primary" @click="open()"><ElIcon><Plus /></ElIcon>{{ t('create') }}</ElButton></template>
    <ElTable :data="rows" class="gm-table" size="small" stripe>
      <ElTableColumn prop="vendor" :label="t('adm_vendor')" width="140" />
      <ElTableColumn prop="name" :label="t('name')" min-width="180" />
      <ElTableColumn prop="version" :label="t('version')" width="100" />
      <ElTableColumn prop="slug" label="Slug" min-width="200" />
      <ElTableColumn prop="releasedOn" :label="t('release_date')" width="120" />
      <ElTableColumn prop="appCount" :label="t('stat_apps')" width="80" align="right" />
      <ElTableColumn :label="t('active')" width="80" align="center"><template #default="{ row }">{{ row.isActive ? '✔' : '—' }}</template></ElTableColumn>
      <ElTableColumn width="160" align="right"><template #default="{ row }"><ElButton size="small" @click="open(row as AdminLlmModelDto)">{{ t('edit') }}</ElButton><ElButton size="small" type="danger" text @click="remove(row as AdminLlmModelDto)">{{ t('delete') }}</ElButton></template></ElTableColumn>
    </ElTable>
    <ElDialog v-model="dialog" :title="form.id ? t('edit') : t('create')" width="520px">
      <ElForm label-position="top" class="grid-form">
        <ElFormItem :label="t('adm_vendor')"><ElInput v-model="form.vendor" /></ElFormItem>
        <ElFormItem :label="t('name')"><ElInput v-model="form.name" /></ElFormItem>
        <ElFormItem :label="t('version')"><ElInput v-model="form.version" /></ElFormItem>
        <ElFormItem :label="t('release_date')"><ElDatePicker v-model="form.releasedOn" type="date" value-format="YYYY-MM-DD" style="width: 100%" /></ElFormItem>
        <ElFormItem :label="t('active')"><ElSwitch v-model="form.isActive" /></ElFormItem>
        <ElFormItem :label="t('adm_sort')"><ElInputNumber v-model="form.sortOrder" /></ElFormItem>
      </ElForm>
      <template #footer><ElButton @click="dialog = false">{{ t('cancel') }}</ElButton><ElButton type="primary" :loading="saving" @click="save">{{ t('save') }}</ElButton></template>
    </ElDialog>
  </AdminPage>
</template>
