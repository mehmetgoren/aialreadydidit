<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import { AdminContentService } from '@/utils/services/admin-service'
import type { AdminBannerDto } from '@/utils/models/admin-models'
import { assetUrl, confirmX, enableAfter, notifyError, notifyS } from '@/utils/tools'

const { t } = useI18n()
const service = new AdminContentService()
const rows = ref<AdminBannerDto[]>([])
const loading = ref(true)
const dialog = ref(false)
const saving = ref(false)
const form = reactive<Partial<AdminBannerDto>>({})
const fileInput = ref<HTMLInputElement | null>(null)
const uploadFor = ref<number | null>(null)

async function load() { loading.value = true; rows.value = await service.banners().catch((e) => { notifyError(e); return [] }); loading.value = false }
onMounted(load)
function open(b?: AdminBannerDto) { Object.assign(form, b ? { ...b } : { id: undefined, title: '', subtitle: '', link: '', position: 'hero', sortOrder: rows.value.length + 1, isActive: true, startsAt: null, endsAt: null }); dialog.value = true }
async function save() { await enableAfter(saving, async () => { try { await service.saveBanner(form.id ?? null, form); notifyS(t('saved')); dialog.value = false; load() } catch (err) { notifyError(err) } }) }
function pickImage(b: AdminBannerDto) { uploadFor.value = b.id; fileInput.value?.click() }
async function onFile(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file || !uploadFor.value) return
  await service.uploadBannerImage(uploadFor.value, file).then(load).catch(notifyError)
  if (fileInput.value) fileInput.value.value = ''
}
async function remove(b: AdminBannerDto) { if (!(await confirmX(t('adm_confirm_delete')))) return; await service.deleteBanner(b.id).then(load).catch(notifyError) }
</script>

<template>
  <AdminPage :title="t('adm_content_banners')" :subtitle="t('adm_banners_subtitle')" :loading="loading">
    <template #actions><ElButton type="primary" @click="open()"><ElIcon><Plus /></ElIcon>{{ t('create') }}</ElButton></template>
    <input ref="fileInput" type="file" accept="image/*" hidden @change="onFile" />
    <ElTable :data="rows" class="gm-table" size="small">
      <ElTableColumn :label="t('adm_image')" width="140"><template #default="{ row }"><img v-if="row.imageUrl" :src="assetUrl(row.imageUrl)" style="width: 120px; height: 50px; object-fit: cover; border-radius: 6px" alt="" /><span v-else class="gm-muted">—</span></template></ElTableColumn>
      <ElTableColumn prop="title" :label="t('title')" min-width="200"><template #default="{ row }"><strong>{{ row.title }}</strong><div class="sub">{{ row.subtitle }}</div></template></ElTableColumn>
      <ElTableColumn prop="link" :label="t('adm_link')" min-width="160" show-overflow-tooltip />
      <ElTableColumn prop="position" :label="t('adm_position')" width="100" />
      <ElTableColumn prop="sortOrder" :label="t('adm_sort')" width="70" align="center" />
      <ElTableColumn :label="t('active')" width="80" align="center"><template #default="{ row }">{{ row.isActive ? '✔' : '—' }}</template></ElTableColumn>
      <ElTableColumn width="240" align="right"><template #default="{ row }"><ElButton size="small" @click="pickImage(row as AdminBannerDto)">{{ t('adm_image') }}</ElButton><ElButton size="small" @click="open(row as AdminBannerDto)">{{ t('edit') }}</ElButton><ElButton size="small" type="danger" text @click="remove(row as AdminBannerDto)">{{ t('delete') }}</ElButton></template></ElTableColumn>
    </ElTable>
    <ElDialog v-model="dialog" :title="form.id ? t('edit') : t('create')" width="560px">
      <ElForm label-position="top" class="grid-form">
        <ElFormItem :label="t('title')" class="full"><ElInput v-model="form.title" maxlength="160" /></ElFormItem>
        <ElFormItem :label="t('adm_subtitle')" class="full"><ElInput v-model="form.subtitle" maxlength="300" /></ElFormItem>
        <ElFormItem :label="t('adm_link')" class="full"><ElInput v-model="form.link" /></ElFormItem>
        <ElFormItem :label="t('adm_position')"><ElSelect v-model="form.position" style="width: 100%"><ElOption value="hero" label="hero" /><ElOption value="sidebar" label="sidebar" /></ElSelect></ElFormItem>
        <ElFormItem :label="t('adm_sort')"><ElInputNumber v-model="form.sortOrder" /></ElFormItem>
        <ElFormItem :label="t('adm_starts_at')"><ElDatePicker v-model="form.startsAt" type="datetime" style="width: 100%" /></ElFormItem>
        <ElFormItem :label="t('adm_ends_at')"><ElDatePicker v-model="form.endsAt" type="datetime" style="width: 100%" /></ElFormItem>
        <ElFormItem :label="t('active')"><ElSwitch v-model="form.isActive" /></ElFormItem>
      </ElForm>
      <template #footer><ElButton @click="dialog = false">{{ t('cancel') }}</ElButton><ElButton type="primary" :loading="saving" @click="save">{{ t('save') }}</ElButton></template>
    </ElDialog>
  </AdminPage>
</template>
