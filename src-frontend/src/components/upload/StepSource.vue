<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { AppDraft, RepositoryInspection } from '@/utils/models/apps-models'
import { MyAppsService } from '@/utils/services/my-apps-service'
import { useSiteStore } from '@/stores/site-store'
import { confirmX, enableAfter, notifyError, notifyS } from '@/utils/tools'
import { formatBytes, formatNumber } from '@/utils/format'

/** Step 1 — source code: a public GitHub/GitLab repository OR a source archive. Never both. */
const props = defineProps<{ draft: AppDraft }>()
const emit = defineEmits<{ updated: [d: AppDraft]; next: [] }>()
const { t } = useI18n()
const site = useSiteStore()
const service = new MyAppsService()

const mode = ref<'repository' | 'archive'>(props.draft.repoUrl ? 'repository' : props.draft.source.analyzedAt ? 'archive' : 'repository')
const repo = reactive({ url: props.draft.repoUrl ?? '', sourceRef: props.draft.draftVersion?.sourceRef ?? '', prefill: !props.draft.publishedAt })
const inspecting = ref(false)
const attaching = ref(false)
const uploading = ref(false)
const progress = ref(0)
const inspection = ref<RepositoryInspection | null>(null)
const fileInput = ref<HTMLInputElement | null>(null)
const hasSource = computed(() => Boolean(props.draft.draftVersion?.files.some((f) => f.kind === 'source')))
const editable = computed(() => props.draft.draftVersion?.status === 'draft' || props.draft.draftVersion?.status === 'rejected')

async function inspect() {
  await enableAfter(inspecting, async () => {
    try {
      inspection.value = await service.inspectRepository(repo.url)
    } catch (err) {
      notifyError(err)
    }
  })
}

async function attach() {
  if (hasSource.value && props.draft.sourceKind === 'archive' && !(await confirmX(t('confirm_replace_archive')))) return
  await enableAfter(attaching, async () => {
    try {
      emit('updated', await service.attachRepository(props.draft.id, repo.url, repo.sourceRef || null, repo.prefill))
      notifyS(t('repo_attached'))
    } catch (err) {
      notifyError(err)
    }
  })
}

async function onFile(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file) return
  if (props.draft.repoUrl && !(await confirmX(t('confirm_replace_repo')))) return
  await enableAfter(uploading, async () => {
    try {
      emit('updated', await service.uploadArchive(props.draft.id, file, (p) => (progress.value = p)))
      notifyS(t('archive_uploaded'))
    } catch (err) {
      notifyError(err)
    } finally {
      if (fileInput.value) fileInput.value.value = ''
    }
  })
}

async function removeSource() {
  if (!(await confirmX(t('confirm_remove_source')))) return
  try {
    emit('updated', await service.removeSource(props.draft.id))
  } catch (err) {
    notifyError(err)
  }
}
</script>

<template>
  <div class="step">
    <p class="step__intro">{{ t('source_intro') }}</p>
    <ElAlert type="info" :closable="false" show-icon :title="t('rule_open_source')" :description="t('source_rule_hint')" style="margin-bottom: 16px" />

    <div v-if="draft.source.analyzedAt || draft.repoUrl" class="gm-card step__current">
      <div class="step__current-head">
        <strong>{{ draft.sourceKind === 'repository' ? t('source_repository') : t('source_archive') }}</strong>
        <ElButton v-if="editable" size="small" text type="danger" @click="removeSource">{{ t('remove') }}</ElButton>
      </div>
      <a v-if="draft.repoUrl" :href="draft.repoUrl" target="_blank" rel="noopener" class="gm-link">{{ draft.repoUrl }}</a>
      <div v-if="draft.source.analyzedAt" class="step__analysis">
        <span>{{ formatNumber(draft.source.lineCount) }} {{ t('lines') }}</span><span>{{ draft.source.fileCount }} {{ t('files') }}</span><span>{{ formatBytes(draft.source.bytes) }}</span>
        <span v-if="draft.source.primaryLanguage">{{ draft.source.primaryLanguage }}</span>
        <ElTag :type="draft.source.licenseMatches ? 'success' : 'danger'" size="small">{{ draft.source.hasLicenseFile ? (draft.source.detectedLicenseSpdxId ? `LICENSE: ${draft.source.detectedLicenseSpdxId}` : t('license_unrecognised')) : t('no_license_file') }}</ElTag>
      </div>
      <div v-else class="gm-muted">{{ draft.source.warnings || t('source_pending') }}</div>
      <ElAlert v-if="draft.source.warnings && draft.source.analyzedAt" type="warning" :closable="false" :title="draft.source.warnings" style="margin-top: 8px" />
    </div>

    <template v-if="editable">
      <ElRadioGroup v-model="mode" class="step__mode">
        <ElRadioButton value="repository">{{ t('source_repository') }}</ElRadioButton>
        <ElRadioButton value="archive">{{ t('source_archive') }}</ElRadioButton>
      </ElRadioGroup>

      <div v-if="mode === 'repository'" class="step__panel">
        <ElForm label-position="top">
          <ElFormItem :label="t('repo_url')">
            <ElInput v-model.trim="repo.url" placeholder="https://github.com/owner/repository" size="large" @keyup.enter="inspect">
              <template #append><ElButton :loading="inspecting" @click="inspect">{{ t('inspect') }}</ElButton></template>
            </ElInput>
          </ElFormItem>
          <div v-if="inspection" class="gm-card step__inspection">
            <div class="step__inspection-grid">
              <span>{{ t('repository') }}</span><b>{{ inspection.owner }}/{{ inspection.name }} <small class="gm-muted">({{ inspection.provider }} · ★ {{ inspection.stars }})</small></b>
              <span>{{ t('description') }}</span><b>{{ inspection.description || '—' }}</b>
              <span>{{ t('language') }}</span><b>{{ inspection.primaryLanguage || '—' }}</b>
              <span>{{ t('license') }}</span><b><ElTag :type="inspection.licenseId ? 'success' : 'danger'" size="small">{{ inspection.detectedLicenseSpdxId || inspection.licenseSpdxId || t('no_license_file') }}</ElTag></b>
              <span>README</span><b>{{ inspection.hasReadme ? t('yes') : t('no') }}</b>
              <span>{{ t('releases') }}</span><b>{{ inspection.releases.length }} <small v-if="inspection.releases.length" class="gm-muted">({{ inspection.releases.map((r) => r.tag).slice(0, 5).join(', ') }})</small></b>
            </div>
            <ElAlert v-for="w in inspection.warnings" :key="w" type="warning" :closable="false" :title="w" style="margin-top: 6px" />
          </div>
          <ElFormItem :label="t('source_ref')"><ElInput v-model.trim="repo.sourceRef" :placeholder="inspection?.defaultBranch || 'main / v1.0.0 / commit'" /></ElFormItem>
          <ElCheckbox v-if="!draft.publishedAt" v-model="repo.prefill">{{ t('prefill_from_repo') }}</ElCheckbox>
        </ElForm>
        <ElButton type="primary" size="large" :loading="attaching" :disabled="!repo.url" @click="attach">{{ t('attach_repository') }}</ElButton>
      </div>

      <div v-else class="step__panel">
        <p class="gm-muted">{{ t('archive_hint', { size: formatBytes(site.limits?.maxSourceArchiveBytes) }) }}</p>
        <input ref="fileInput" type="file" accept=".zip,.tar.gz,.tgz,.tar" hidden @change="onFile" />
        <ElButton type="primary" size="large" :loading="uploading" @click="fileInput?.click()"><ElIcon><Upload /></ElIcon>{{ t('choose_archive') }}</ElButton>
        <ElProgress v-if="uploading" :percentage="progress" style="margin-top: 10px" />
      </div>
    </template>
    <ElAlert v-else type="info" :closable="false" :title="t('source_locked')" />

    <div class="step__nav"><span /><ElButton type="primary" @click="emit('next')">{{ t('next') }} →</ElButton></div>
  </div>
</template>

<style scoped lang="scss">
.step {
  &__intro { color: var(--gm-text-muted); margin: 0 0 12px; }
  &__current { padding: 14px 16px; margin-bottom: 16px; &-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 4px; } }
  &__analysis { display: flex; gap: 14px; flex-wrap: wrap; font-size: 13px; margin-top: 6px; }
  &__mode { margin-bottom: 14px; }
  &__panel { max-width: 720px; }
  &__inspection { padding: 12px 14px; margin-bottom: 14px; &-grid { display: grid; grid-template-columns: auto 1fr; gap: 4px 14px; font-size: 13px; span { color: var(--gm-text-muted); } } }
  &__nav { display: flex; justify-content: space-between; margin-top: 24px; padding-top: 16px; border-top: 1px solid var(--gm-border); }
}
</style>
