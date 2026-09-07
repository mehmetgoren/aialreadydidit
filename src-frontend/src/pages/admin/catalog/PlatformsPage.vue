<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import { AdminCatalogService } from '@/utils/services/admin-service'
import type { AdminPlatformDto } from '@/utils/models/admin-models'
import { enableAfter, notifyError, notifyS, platformIcon } from '@/utils/tools'

const { t } = useI18n()
const service = new AdminCatalogService()
const rows = ref<AdminPlatformDto[]>([])
const loading = ref(true)
const dialog = ref(false)
const saving = ref(false)
const form = reactive<Partial<AdminPlatformDto>>({})

async function load() {
  loading.value = true
  rows.value = await service.platforms().catch((e) => { notifyError(e); return [] })
  loading.value = false
}
onMounted(load)
function open(row?: AdminPlatformDto) {
  Object.assign(form, row ? { ...row } : { id: undefined, code: '', name: '', icon: '', allowedExtensions: '', allowsExternalReference: false, installHint: '', sortOrder: rows.value.length + 1, isActive: true })
  dialog.value = true
}
async function save() {
  await enableAfter(saving, async () => {
    try {
      await service.savePlatform(form.id ?? null, form)
      notifyS(t('saved'))
      dialog.value = false
      load()
    } catch (err) {
      notifyError(err)
    }
  })
}
</script>

<template>
  <AdminPage :title="t('adm_platforms')" :subtitle="t('adm_platforms_subtitle')" :loading="loading">
    <template #actions><ElButton type="primary" @click="open()"><ElIcon><Plus /></ElIcon>{{ t('create') }}</ElButton></template>
    <ElTable :data="rows" class="gm-table" size="small" stripe>
      <ElTableColumn :label="t('platform')" width="160"><template #default="{ row }">{{ platformIcon(row.code) }} {{ row.name }} <code class="sub">{{ row.code }}</code></template></ElTableColumn>
      <ElTableColumn prop="allowedExtensions" :label="t('accepted_extensions')" min-width="260" />
      <ElTableColumn :label="t('adm_external_ref')" width="110" align="center"><template #default="{ row }">{{ row.allowsExternalReference ? '✔' : '—' }}</template></ElTableColumn>
      <ElTableColumn prop="installHint" :label="t('install_hint')" min-width="240" show-overflow-tooltip />
      <ElTableColumn prop="fileCount" :label="t('files')" width="80" align="right" />
      <ElTableColumn :label="t('active')" width="80" align="center"><template #default="{ row }">{{ row.isActive ? '✔' : '—' }}</template></ElTableColumn>
      <ElTableColumn width="100" align="right"><template #default="{ row }"><ElButton size="small" @click="open(row as AdminPlatformDto)">{{ t('edit') }}</ElButton></template></ElTableColumn>
    </ElTable>
    <ElDialog v-model="dialog" :title="form.id ? t('edit') : t('create')" width="600px">
      <ElForm label-position="top" class="grid-form">
        <ElFormItem :label="t('adm_code')"><ElInput v-model="form.code" :disabled="Boolean(form.id)" /></ElFormItem>
        <ElFormItem :label="t('name')"><ElInput v-model="form.name" /></ElFormItem>
        <ElFormItem :label="t('accepted_extensions')" class="full"><ElInput v-model="form.allowedExtensions" placeholder=".exe,.msi" /></ElFormItem>
        <ElFormItem :label="t('install_hint')" class="full"><ElInput v-model="form.installHint" type="textarea" :rows="2" /></ElFormItem>
        <ElFormItem :label="t('adm_external_ref')"><ElSwitch v-model="form.allowsExternalReference" /></ElFormItem>
        <ElFormItem :label="t('active')"><ElSwitch v-model="form.isActive" /></ElFormItem>
        <ElFormItem :label="t('adm_sort')"><ElInputNumber v-model="form.sortOrder" /></ElFormItem>
      </ElForm>
      <template #footer><ElButton @click="dialog = false">{{ t('cancel') }}</ElButton><ElButton type="primary" :loading="saving" @click="save">{{ t('save') }}</ElButton></template>
    </ElDialog>
  </AdminPage>
</template>
