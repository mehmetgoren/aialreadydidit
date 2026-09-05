<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import type { CascaderOption } from 'element-plus'
import type { ModerationDetail } from '@/utils/models/admin-models'
import { AdminModerationService } from '@/utils/services/admin-service'
import AdminPage from '@/components/admin/AdminPage.vue'
import AdminDetailList from '@/components/admin/AdminDetailList.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import MarkdownView from '@/components/common/MarkdownView.vue'
import ScreenshotGallery from '@/components/catalog/ScreenshotGallery.vue'
import PromptBlock from '@/components/catalog/PromptBlock.vue'
import AppCard from '@/components/catalog/AppCard.vue'
import { useCategoryStore, useCategoryName } from '@/stores/category-store'
import type { CategoryNode } from '@/utils/models/catalog-models'
import { confirmX, enableAfter, notifyError, notifyS, platformIcon } from '@/utils/tools'
import { formatBytes, formatDateTime, formatNumber } from '@/utils/format'

/** Moderation review: everything an admin needs to approve or reject on one page. */
const route = useRoute()
const { t } = useI18n()
const service = new AdminModerationService()
const categories = useCategoryStore()
const name = useCategoryName()
const appId = Number(route.params.appId)
const data = ref<ModerationDetail | null>(null)
const loading = ref(true)
const busy = ref(false)
const decision = reactive({ note: '', categoryId: null as number | null, feature: false })
const pendingVersion = computed(() => data.value?.draft.draftVersion ?? null)
const cascader = computed(() => {
  const map = (nodes: CategoryNode[]): CascaderOption[] => nodes.map((n) => ({ value: n.id, label: name(n), children: n.children.length ? map(n.children) : undefined }))
  return map(categories.roots)
})
const categoryPath = computed(() => (decision.categoryId ? categories.pathToId(decision.categoryId).map((c) => c.id) : []))

async function load() {
  loading.value = true
  try {
    await categories.fetchTree()
    data.value = await service.detail(appId)
    decision.categoryId = data.value.llmSuggestedCategory?.id && data.value.llmSuggestedCategory.id !== data.value.draft.categoryId ? null : data.value.draft.categoryId
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
}
onMounted(load)

async function run(action: () => Promise<ModerationDetail>, message: string) {
  await enableAfter(busy, async () => {
    try {
      data.value = await action()
      notifyS(message)
    } catch (err) {
      notifyError(err)
    }
  })
}
const approve = () => run(() => service.approve(appId, { note: decision.note || null, versionId: pendingVersion.value?.id ?? null, categoryId: decision.categoryId, feature: decision.feature }), t('adm_approved'))
async function reject() {
  if (!decision.note.trim()) return notifyError(new Error(t('adm_reject_needs_note')))
  if (!(await confirmX(t('adm_confirm_reject')))) return
  await run(() => service.reject(appId, { note: decision.note, versionId: pendingVersion.value?.id ?? null }), t('adm_rejected'))
}
const rescan = () => run(() => service.rescan(appId, pendingVersion.value?.id ?? null), t('adm_rescan_queued'))
const verifyLicense = () => run(() => service.verifyLicense(appId, decision.note || undefined), t('saved'))
const addNote = () => run(() => service.note(appId, decision.note, pendingVersion.value?.id ?? null), t('saved'))
</script>

<template>
  <AdminPage :title="data ? `${t('adm_review')}: ${data.draft.name}` : t('adm_review')" :loading="loading">
    <template #actions>
      <RouterLink to="/admin/moderation"><ElButton>← {{ t('adm_moderation_queue') }}</ElButton></RouterLink>
      <RouterLink v-if="data" :to="`/app/${data.draft.slug}`" target="_blank"><ElButton>{{ t('view_page') }}</ElButton></RouterLink>
      <RouterLink v-if="data" :to="`/admin/apps/${appId}`"><ElButton>{{ t('edit') }}</ElButton></RouterLink>
    </template>
    <div v-if="data" class="rv">
      <div class="rv__main">
        <div class="rv__status"><StatusTag :value="data.draft.status" /><StatusTag v-if="pendingVersion" :value="pendingVersion.status" /><span v-if="pendingVersion" class="gm-muted">v{{ pendingVersion.version }}</span><span v-if="data.draft.rejectionReason" class="rv__reason">{{ data.draft.rejectionReason }}</span></div>
        <ElAlert v-if="data.licenseIssue" type="error" :closable="false" show-icon :title="data.licenseIssue" style="margin-bottom: 10px" />
        <ElAlert v-if="data.reports.length" type="warning" :closable="false" show-icon :title="t('adm_has_reports', { n: data.reports.length })" style="margin-bottom: 10px" />
        <ScreenshotGallery :screenshots="data.preview.screenshots" />
        <ElTabs style="margin-top: 12px">
          <ElTabPane :label="t('tab_description')"><MarkdownView :source="data.preview.longDescription" /></ElTabPane>
          <ElTabPane v-if="data.preview.readmeMarkdown" label="README"><MarkdownView :source="data.preview.readmeMarkdown" /></ElTabPane>
          <ElTabPane :label="`${t('tab_prompts')} (${data.preview.prompts.length})`"><PromptBlock :prompts="data.preview.prompts" :app-name="data.preview.name" :repo-url="data.preview.repoUrl" /></ElTabPane>
          <ElTabPane :label="`${t('files')} (${pendingVersion?.files.length ?? 0})`">
            <ElTable :data="pendingVersion?.files ?? []" size="small" class="gm-table">
              <ElTableColumn :label="t('file')" min-width="220"><template #default="{ row }">{{ platformIcon(row.platformCode) }} {{ row.fileName }}</template></ElTableColumn>
              <ElTableColumn :label="t('adm_kind')" prop="kind" width="110" />
              <ElTableColumn :label="t('size')" width="100"><template #default="{ row }">{{ formatBytes(row.sizeBytes) }}</template></ElTableColumn>
              <ElTableColumn :label="t('scan')" width="140"><template #default="{ row }"><StatusTag :value="row.scanStatus" /> <small v-if="row.scanSignature" class="gm-muted">{{ row.scanSignature }}</small></template></ElTableColumn>
              <ElTableColumn label="sha256" min-width="200"><template #default="{ row }"><code style="font-size: 11px">{{ row.sha256 }}</code></template></ElTableColumn>
              <ElTableColumn width="110"><template #default="{ row }"><a :href="row.downloadUrl" target="_blank" rel="noopener" class="gm-link">{{ t('download') }}</a></template></ElTableColumn>
            </ElTable>
            <h4>{{ t('adm_scan_history') }}</h4>
            <ElTable :data="data.scanResults" size="small" class="gm-table">
              <ElTableColumn :label="t('file')" prop="fileName" min-width="200" />
              <ElTableColumn :label="t('adm_verdict')" width="120"><template #default="{ row }"><StatusTag :value="row.verdict" /></template></ElTableColumn>
              <ElTableColumn prop="signature" :label="t('adm_signature')" width="180" />
              <ElTableColumn :label="t('date')" width="160"><template #default="{ row }">{{ formatDateTime(row.scannedAt) }}</template></ElTableColumn>
            </ElTable>
          </ElTabPane>
          <ElTabPane :label="`${t('similar_apps')} (${data.similarApps.length})`">
            <p class="gm-muted">{{ t('adm_similar_hint') }}</p>
            <div class="gm-grid"><AppCard v-for="a in data.similarApps" :key="a.id" :app="a" show-similarity /></div>
          </ElTabPane>
          <ElTabPane :label="`${t('adm_history')} (${data.history.length})`">
            <ElTimeline>
              <ElTimelineItem v-for="h in data.history" :key="h.id" :timestamp="formatDateTime(h.createdAt)"><strong>{{ h.action }}</strong> · {{ h.adminUsername }}<div v-if="h.note">{{ h.note }}</div></ElTimelineItem>
            </ElTimeline>
          </ElTabPane>
          <ElTabPane v-if="data.reports.length" :label="`${t('adm_reports')} (${data.reports.length})`">
            <div v-for="r in data.reports" :key="r.id" class="rv__report"><StatusTag :value="r.status" /> <strong>{{ t(`report_reason_${r.reason}`) }}</strong> · {{ r.reporterUsername ?? r.reporterEmail ?? t('anonymous') }} · {{ formatDateTime(r.createdAt) }}<p>{{ r.details }}</p></div>
          </ElTabPane>
        </ElTabs>
      </div>

      <aside class="rv__side">
        <div class="gm-card rv__box">
          <h4>{{ t('adm_decision') }}</h4>
          <ElForm label-position="top">
            <ElFormItem :label="t('category')">
              <ElCascader :model-value="categoryPath" :options="cascader" :props="{ checkStrictly: true }" style="width: 100%" @update:model-value="decision.categoryId = ($event as number[] | null)?.at(-1) ?? null" />
              <div v-if="data.llmSuggestedCategory" class="sub">🤖 {{ t('adm_llm_suggests') }}: <a class="gm-link" @click="decision.categoryId = data!.llmSuggestedCategory!.id">{{ name(data.llmSuggestedCategory) }}</a><span v-if="data.llmSuggestedCategoryProposedName"> ({{ t('adm_proposed_new') }})</span></div>
            </ElFormItem>
            <ElFormItem :label="t('adm_note_to_uploader')"><ElInput v-model="decision.note" type="textarea" :rows="3" /></ElFormItem>
            <ElCheckbox v-model="decision.feature">{{ t('adm_feature_on_approve') }}</ElCheckbox>
          </ElForm>
          <div class="rv__buttons">
            <ElButton type="success" :loading="busy" @click="approve">{{ t('adm_approve') }}</ElButton>
            <ElButton type="danger" :loading="busy" @click="reject">{{ t('adm_reject') }}</ElButton>
            <ElButton :loading="busy" @click="rescan">{{ t('adm_rescan') }}</ElButton>
            <ElButton v-if="data.licenseIssue" :loading="busy" @click="verifyLicense">{{ t('adm_verify_license') }}</ElButton>
            <ElButton text :loading="busy" :disabled="!decision.note" @click="addNote">{{ t('adm_add_note') }}</ElButton>
          </div>
        </div>
        <div class="gm-card rv__box">
          <h4>{{ t('adm_summary') }}</h4>
          <AdminDetailList :columns="1" :border="false" :items="[
            { label: t('license'), value: `${data.draft.licenseSpdxId} — ${data.draft.source.detectedLicenseSpdxId ? 'LICENSE ' + data.draft.source.detectedLicenseSpdxId : t('no_license_file')}` },
            { label: t('source_code'), value: data.draft.repoUrl ?? t('source_archive'), href: data.draft.repoUrl ?? undefined },
            { label: t('source_size'), value: `${formatNumber(data.draft.source.lineCount)} ${t('lines')} · ${data.draft.source.fileCount} ${t('files')}` },
            { label: t('language'), value: data.draft.source.primaryLanguage },
            { label: t('generated_by'), value: data.preview.llmModel?.displayName },
            { label: t('cost_title'), value: `${formatNumber(data.draft.estGenerationTokens)} ${t('tokens')}` },
            { label: t('tags'), value: data.draft.tags.join(', ') },
            { label: t('submitted'), value: data.draft.submittedAt, type: 'datetime' },
          ]" />
        </div>
        <div class="gm-card rv__box">
          <h4>{{ t('uploader') }}</h4>
          <AdminDetailList :columns="1" :border="false" :items="[
            { label: t('username'), value: data.uploader.username, to: `/admin/users/${data.uploader.id}` },
            { label: t('email'), value: `${data.uploader.email}${data.uploader.emailVerified ? ' ✔' : ''}` },
            { label: t('adm_trust_level'), value: data.uploader.trustLevel },
            { label: t('adm_apps_published'), value: `${data.uploader.publishedAppCount} / ${data.uploader.appCount} (${data.uploader.rejectedAppCount} ${t('status_rejected').toLowerCase()})` },
            { label: t('adm_reports_against'), value: data.uploader.reportCount },
            { label: t('member_since'), value: data.uploader.createdAt, type: 'date' },
          ]" />
        </div>
      </aside>
    </div>
  </AdminPage>
</template>

<style scoped lang="scss">
.rv {
  display: grid;
  grid-template-columns: 1fr 380px;
  gap: 16px;
  &__main { min-width: 0; }
  &__status { display: flex; gap: 8px; align-items: center; margin-bottom: 10px; flex-wrap: wrap; }
  &__reason { color: var(--el-color-danger); }
  &__box { padding: 14px; margin-bottom: 12px; h4 { margin: 0 0 8px; } }
  &__buttons { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 8px; .el-button + .el-button { margin-left: 0; } }
  &__report { padding: 8px 0; border-top: 1px solid var(--gm-border); p { margin: 4px 0 0; } }
  @media (max-width: 1100px) { grid-template-columns: 1fr; }
}
</style>
