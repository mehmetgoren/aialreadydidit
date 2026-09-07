<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import { AdminCatalogService } from '@/utils/services/admin-service'
import type { AdminLicenseDto } from '@/utils/models/admin-models'
import { confirmX, enableAfter, notifyError, notifyS } from '@/utils/tools'

const { t } = useI18n()
const service = new AdminCatalogService()
const rows = ref<AdminLicenseDto[]>([])
const loading = ref(true)
const dialog = ref(false)
const saving = ref(false)
const form = reactive<Partial<AdminLicenseDto>>({})

async function load() {
  loading.value = true
  rows.value = await service.licenses().catch((e) => { notifyError(e); return [] })
  loading.value = false
}
onMounted(load)
function open(row?: AdminLicenseDto) {
  Object.assign(form, row ? { ...row } : { id: undefined, spdxId: '', name: '', url: '', family: '', isOsiApproved: true, isFsfLibre: true, isAllowed: true, sortOrder: rows.value.length + 1 })
  dialog.value = true
}
async function save() {
  await enableAfter(saving, async () => {
    try {
      await service.saveLicense(form.id ?? null, form)
      notifyS(t('saved'))
      dialog.value = false
      load()
    } catch (err) {
      notifyError(err)
    }
  })
}
async function remove(row: AdminLicenseDto) {
  if (!(await confirmX(t('adm_confirm_delete')))) return
  await service.deleteLicense(row.id).then(load).catch(notifyError)
}
</script>

<template>
  <AdminPage :title="t('adm_licenses')" :subtitle="t('adm_licenses_subtitle')" :loading="loading">
    <template #actions><ElButton type="primary" @click="open()"><ElIcon><Plus /></ElIcon>{{ t('create') }}</ElButton></template>
    <ElTable :data="rows" class="gm-table" size="small" stripe>
      <ElTableColumn prop="spdxId" label="SPDX" width="170" />
      <ElTableColumn prop="name" :label="t('name')" min-width="240" />
      <ElTableColumn prop="family" :label="t('adm_family')" width="120" />
      <ElTableColumn label="OSI" width="70" align="center"><template #default="{ row }">{{ row.isOsiApproved ? '✔' : '—' }}</template></ElTableColumn>
      <ElTableColumn label="FSF" width="70" align="center"><template #default="{ row }">{{ row.isFsfLibre ? '✔' : '—' }}</template></ElTableColumn>
      <ElTableColumn :label="t('adm_allowed')" width="100" align="center"><template #default="{ row }"><ElTag :type="row.isAllowed ? 'success' : 'danger'" size="small">{{ row.isAllowed ? t('yes') : t('no') }}</ElTag></template></ElTableColumn>
      <ElTableColumn prop="appCount" :label="t('stat_apps')" width="80" align="right" />
      <ElTableColumn width="160" align="right"><template #default="{ row }"><ElButton size="small" @click="open(row as AdminLicenseDto)">{{ t('edit') }}</ElButton><ElButton size="small" type="danger" text @click="remove(row as AdminLicenseDto)">{{ t('delete') }}</ElButton></template></ElTableColumn>
    </ElTable>
    <ElDialog v-model="dialog" :title="form.id ? t('edit') : t('create')" width="560px">
      <ElForm label-position="top" class="grid-form">
        <ElFormItem label="SPDX id"><ElInput v-model="form.spdxId" /></ElFormItem>
        <ElFormItem :label="t('name')"><ElInput v-model="form.name" /></ElFormItem>
        <ElFormItem label="URL" class="full"><ElInput v-model="form.url" /></ElFormItem>
        <ElFormItem :label="t('adm_family')"><ElInput v-model="form.family" /></ElFormItem>
        <ElFormItem :label="t('adm_sort')"><ElInputNumber v-model="form.sortOrder" /></ElFormItem>
        <ElFormItem label="OSI"><ElSwitch v-model="form.isOsiApproved" /></ElFormItem>
        <ElFormItem label="FSF"><ElSwitch v-model="form.isFsfLibre" /></ElFormItem>
        <ElFormItem :label="t('adm_allowed')"><ElSwitch v-model="form.isAllowed" /></ElFormItem>
      </ElForm>
      <template #footer><ElButton @click="dialog = false">{{ t('cancel') }}</ElButton><ElButton type="primary" :loading="saving" @click="save">{{ t('save') }}</ElButton></template>
    </ElDialog>
  </AdminPage>
</template>
