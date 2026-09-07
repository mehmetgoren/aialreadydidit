<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import type { CascaderOption } from 'element-plus'
import type { AppDraft } from '@/utils/models/apps-models'
import type { CategoryNode } from '@/utils/models/catalog-models'
import { AdminAppsService } from '@/utils/services/admin-service'
import { useCategoryStore, useCategoryName } from '@/stores/category-store'
import { useSiteStore } from '@/stores/site-store'
import AdminPage from '@/components/admin/AdminPage.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import VersionsList from '@/components/catalog/VersionsList.vue'
import { enableAfter, notifyError, notifyS } from '@/utils/tools'
import MarkdownEditor from '@/components/common/MarkdownEditor.vue'

/** Admin edit of any app: texts, category, license, model, featured, slug, license override. Files/screenshots stay with the uploader's editor. */
const route = useRoute()
const { t } = useI18n()
const service = new AdminAppsService()
const categories = useCategoryStore()
const site = useSiteStore()
const name = useCategoryName()
const id = Number(route.params.id)
const app = ref<AppDraft | null>(null)
const loading = ref(true)
const saving = ref(false)
const form = reactive({ name: '', slug: '', shortDescription: '', longDescription: '', categoryId: null as number | null, licenseId: null as number | null, llmModelId: null as number | null, llmModelNote: '', tags: [] as string[], isFeatured: false, featuredOrder: 0, featuredNote: '', licenseVerifiedByAdmin: false })
const cascader = computed(() => {
  const map = (nodes: CategoryNode[]): CascaderOption[] => nodes.map((n) => ({ value: n.id, label: name(n), children: n.children.length ? map(n.children) : undefined }))
  return map(categories.roots)
})
const categoryPath = computed(() => (form.categoryId ? categories.pathToId(form.categoryId).map((c) => c.id) : []))

onMounted(async () => {
  try {
    await Promise.all([categories.fetchTree(), site.ensureLoaded()])
    app.value = await service.getOne(id)
    Object.assign(form, { name: app.value.name, slug: app.value.slug, shortDescription: app.value.shortDescription, longDescription: app.value.longDescription, categoryId: app.value.categoryId, licenseId: app.value.licenseId, llmModelId: app.value.llmModelId, llmModelNote: app.value.llmModelNote ?? '', tags: [...app.value.tags], isFeatured: false, featuredOrder: 0, featuredNote: '', licenseVerifiedByAdmin: app.value.source.licenseMatches })
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
})

async function save() {
  await enableAfter(saving, async () => {
    try {
      app.value = await service.update(id, { ...form, featuredOrder: form.isFeatured ? form.featuredOrder : null })
      notifyS(t('saved'))
    } catch (err) {
      notifyError(err)
    }
  })
}
async function recompute() {
  try {
    await service.recompute(id)
    notifyS(t('adm_recompute_queued'))
  } catch (err) {
    notifyError(err)
  }
}
</script>

<template>
  <AdminPage :title="app ? app.name : t('adm_apps')" :loading="loading">
    <template #actions>
      <RouterLink to="/admin/apps"><ElButton>← {{ t('adm_apps') }}</ElButton></RouterLink>
      <RouterLink v-if="app" :to="`/admin/moderation/${id}`"><ElButton>{{ t('adm_review') }}</ElButton></RouterLink>
      <RouterLink v-if="app" :to="`/upload/${id}`"><ElButton>{{ t('adm_open_editor') }}</ElButton></RouterLink>
      <RouterLink v-if="app" :to="`/app/${app.slug}`" target="_blank"><ElButton>{{ t('view_page') }}</ElButton></RouterLink>
      <ElButton @click="recompute">{{ t('adm_recompute') }}</ElButton>
    </template>
    <template v-if="app">
      <div style="margin-bottom: 12px"><StatusTag :value="app.status" /> <span class="gm-muted">{{ app.slug }} · {{ app.downloadCount }} {{ t('downloads').toLowerCase() }} · {{ app.viewCount }} {{ t('views') }}</span></div>
      <ElForm label-position="top" class="grid-form">
        <ElFormItem :label="t('app_name_label')"><ElInput v-model="form.name" maxlength="120" /></ElFormItem>
        <ElFormItem label="Slug"><ElInput v-model="form.slug" maxlength="120" /></ElFormItem>
        <ElFormItem :label="t('short_description')" class="full"><ElInput v-model="form.shortDescription" maxlength="200" show-word-limit /></ElFormItem>
        <ElFormItem :label="t('long_description')" class="full"><MarkdownEditor v-model="form.longDescription" :rows="8" /></ElFormItem>
        <ElFormItem :label="t('category')"><ElCascader :model-value="categoryPath" :options="cascader" :props="{ checkStrictly: true }" style="width: 100%" @update:model-value="form.categoryId = ($event as number[] | null)?.at(-1) ?? null" /></ElFormItem>
        <ElFormItem :label="t('license')"><ElSelect v-model="form.licenseId" filterable style="width: 100%"><ElOption v-for="l in site.licenses" :key="l.id" :value="l.id" :label="`${l.spdxId} — ${l.name}`" /></ElSelect></ElFormItem>
        <ElFormItem :label="t('generated_by')"><ElSelect v-model="form.llmModelId" filterable clearable style="width: 100%"><ElOption v-for="m in site.llmModels" :key="m.id" :value="m.id" :label="m.displayName" /></ElSelect></ElFormItem>
        <ElFormItem :label="t('model_note')"><ElInput v-model="form.llmModelNote" maxlength="200" /></ElFormItem>
        <ElFormItem :label="t('tags')" class="full"><ElSelect v-model="form.tags" multiple filterable allow-create default-first-option style="width: 100%" /></ElFormItem>
        <ElFormItem :label="t('adm_featured')"><ElSwitch v-model="form.isFeatured" /> <ElInputNumber v-if="form.isFeatured" v-model="form.featuredOrder" :min="0" size="small" style="margin-left: 10px" /></ElFormItem>
        <ElFormItem :label="t('adm_featured_note')"><ElInput v-model="form.featuredNote" maxlength="200" /></ElFormItem>
        <ElFormItem :label="t('adm_license_verified')" class="full"><ElSwitch v-model="form.licenseVerifiedByAdmin" /> <span class="sub" style="margin-left: 8px">{{ t('adm_license_verified_hint') }} ({{ app.source.detectedLicenseSpdxId ?? t('no_license_file') }})</span></ElFormItem>
      </ElForm>
      <ElButton type="primary" :loading="saving" @click="save">{{ t('save') }}</ElButton>
      <h3 style="margin-top: 24px">{{ t('versions') }}</h3>
      <VersionsList :versions="app.versions" show-status />
    </template>
  </AdminPage>
</template>
