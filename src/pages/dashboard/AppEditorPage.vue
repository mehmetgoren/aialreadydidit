<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import type { AppDraft } from '@/utils/models/apps-models'
import { MyAppsService } from '@/utils/services/my-apps-service'
import { useSiteStore } from '@/stores/site-store'
import { useCategoryStore } from '@/stores/category-store'
import { useCommonStore } from '@/stores/common-store'
import StepSource from '@/components/upload/StepSource.vue'
import StepDetails from '@/components/upload/StepDetails.vue'
import StepFiles from '@/components/upload/StepFiles.vue'
import StepScreenshots from '@/components/upload/StepScreenshots.vue'
import StepReview from '@/components/upload/StepReview.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import { confirmX, notifyError, notifyS } from '@/utils/tools'

/**
 * Upload wizard + editor: Source → Details → Files → Screenshots → Review. The draft is saved on the server after
 * every step, so the member can come back later. The same page edits published apps (new versions, texts).
 */
const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const site = useSiteStore()
const categories = useCategoryStore()
const common = useCommonStore()
const service = new MyAppsService()

const draft = ref<AppDraft | null>(null)
const loading = ref(true)
const step = ref(0)
const steps = ['source', 'details', 'files', 'screenshots', 'review'] as const
const id = computed(() => (route.params.id ? Number(route.params.id) : null))
const isPublished = computed(() => Boolean(draft.value?.publishedAt))
const stepIssues = computed(() => {
  const map: Record<string, number> = {}
  for (const i of draft.value?.readiness.issues ?? []) if (i.blocking) map[i.step] = (map[i.step] ?? 0) + 1
  return map
})

async function load() {
  loading.value = true
  try {
    await Promise.all([site.ensureLoaded(), categories.fetchTree()])
    if (id.value) draft.value = await service.getDraft(id.value)
    common.setPageTitle(draft.value ? draft.value.name : t('upload_app'))
    common.setBreadcrumb([{ label: t('dash_my_apps'), to: '/dashboard/apps' }, { label: draft.value?.name ?? t('upload_app') }])
    if (draft.value && isPublished.value) step.value = 4
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
}
onMounted(load)
watch(id, load)

function onUpdated(d: AppDraft) {
  draft.value = d
  if (!id.value) router.replace(`/upload/${d.id}`)
}

async function ensureDraft(): Promise<AppDraft> {
  if (draft.value) return draft.value
  const d = await service.create()
  onUpdated(d)
  return d
}

let pending: Promise<void> | null = null
onMounted(() => {
  // create the draft up front so every step can upload straight away
  pending = load().then(async () => {
    if (!draft.value && !id.value) await ensureDraft().catch(notifyError)
  })
})

async function refresh() {
  if (!draft.value) return
  draft.value = await service.getDraft(draft.value.id)
}

async function submit() {
  if (!draft.value) return
  try {
    draft.value = await service.submit(draft.value.id)
    notifyS(t('submitted_ok'))
    step.value = 4
  } catch (err) {
    notifyError(err)
    await refresh()
  }
}

async function withdraw() {
  if (!draft.value || !(await confirmX(t('confirm_withdraw')))) return
  try {
    draft.value = await service.withdraw(draft.value.id)
  } catch (err) {
    notifyError(err)
  }
}

async function remove() {
  if (!draft.value || !(await confirmX(t('confirm_delete_app', { name: draft.value.name })))) return
  try {
    await service.remove(draft.value.id)
    router.push('/dashboard/apps')
  } catch (err) {
    notifyError(err)
  }
}

async function setListed(listed: boolean) {
  if (!draft.value) return
  try {
    draft.value = listed ? await service.relist(draft.value.id) : await service.unlist(draft.value.id)
  } catch (err) {
    notifyError(err)
  }
}
void pending
</script>

<template>
  <div class="gm-container editor">
    <div v-loading="loading" class="gm-card editor__card">
      <header class="editor__head">
        <div>
          <h1 class="gm-title">{{ draft && draft.name !== 'Untitled app' ? draft.name : t('upload_app') }}</h1>
          <div v-if="draft" class="editor__status">
            <StatusTag :value="draft.status" />
            <span v-if="draft.rejectionReason" class="editor__reason">{{ draft.rejectionReason }}</span>
            <span v-if="draft.status === 'pendingScan'" class="gm-muted">· {{ t('scan_in_progress') }}</span>
            <span v-if="draft.status === 'pendingReview'" class="gm-muted">· {{ t('review_in_progress') }}</span>
          </div>
        </div>
        <div v-if="draft" class="editor__actions">
          <RouterLink v-if="isPublished" :to="`/app/${draft.slug}`"><ElButton>{{ t('view_page') }}</ElButton></RouterLink>
          <ElButton v-if="draft.status === 'published'" @click="setListed(false)">{{ t('unlist') }}</ElButton>
          <ElButton v-if="draft.status === 'unlisted'" type="primary" plain @click="setListed(true)">{{ t('relist') }}</ElButton>
          <ElButton v-if="draft.status === 'pendingScan' || draft.status === 'pendingReview'" @click="withdraw">{{ t('withdraw') }}</ElButton>
          <ElButton v-if="!isPublished" type="danger" text @click="remove">{{ t('delete') }}</ElButton>
        </div>
      </header>

      <ElSteps :active="step" finish-status="success" align-center class="editor__steps">
        <ElStep v-for="(s, i) in steps" :key="s" :title="t(`step_${s}`)" :status="stepIssues[s] ? 'error' : i < step ? 'success' : i === step ? 'process' : 'wait'" @click="step = i" />
      </ElSteps>

      <div v-if="draft" class="editor__body">
        <StepSource v-if="step === 0" :draft="draft" @updated="onUpdated" @next="step = 1" />
        <StepDetails v-else-if="step === 1" :draft="draft" @updated="onUpdated" @next="step = 2" @back="step = 0" />
        <StepFiles v-else-if="step === 2" :draft="draft" @updated="onUpdated" @refresh="refresh" @next="step = 3" @back="step = 1" />
        <StepScreenshots v-else-if="step === 3" :draft="draft" @updated="onUpdated" @refresh="refresh" @next="step = 4" @back="step = 2" />
        <StepReview v-else :draft="draft" @go="step = $event" @submit="submit" @refresh="refresh" @updated="onUpdated" />
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.editor {
  padding: 16px 16px 32px;
  &__card { padding: 22px 26px 26px; }
  &__head { display: flex; justify-content: space-between; gap: 12px; flex-wrap: wrap; align-items: flex-start; margin-bottom: 8px; }
  &__status { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; font-size: 13px; }
  &__reason { color: var(--el-color-danger); }
  &__actions { display: flex; gap: 8px; flex-wrap: wrap; .el-button + .el-button { margin-left: 0; } }
  &__steps { margin: 14px 0 22px; :deep(.el-step) { cursor: pointer; } }
}
</style>
