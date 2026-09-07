<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { AppDraft } from '@/utils/models/apps-models'
import { MyAppsService } from '@/utils/services/my-apps-service'
import { useCategoryName } from '@/stores/category-store'
import StatusTag from '@/components/common/StatusTag.vue'
import VersionsList from '@/components/catalog/VersionsList.vue'
import { assetUrl, confirmX, enableAfter, notifyError, notifyS } from '@/utils/tools'
import { formatMoney, formatNumber } from '@/utils/format'

/** Step 5 — readiness checklist + submit; for published apps: versions and "new version". */
const props = defineProps<{ draft: AppDraft }>()
const emit = defineEmits<{ go: [step: number]; submit: []; refresh: []; updated: [d: AppDraft] }>()
const { t } = useI18n()
const name = useCategoryName()
const service = new MyAppsService()
const stepIndex: Record<string, number> = { source: 0, details: 1, files: 2, screenshots: 3 }
const blocking = computed(() => props.draft.readiness.issues.filter((i) => i.blocking))
const warnings = computed(() => props.draft.readiness.issues.filter((i) => !i.blocking))
const isPublished = computed(() => Boolean(props.draft.publishedAt))
const submitting = ref(false)
const newVersion = ref(false)
const savingVersion = ref(false)
const vform = reactive({ version: '', changelog: '', sourceRef: '' })
const inProgress = computed(() => props.draft.versions.find((v) => v.status !== 'published' && v.status !== 'removed'))

async function createVersion() {
  await enableAfter(savingVersion, async () => {
    try {
      emit('updated', await service.createVersion(props.draft.id, { version: vform.version, changelog: vform.changelog || null, sourceRef: vform.sourceRef || null }))
      newVersion.value = false
      notifyS(t('version_created'))
      emit('go', 2)
    } catch (err) {
      notifyError(err)
    }
  })
}

async function submitVersion() {
  if (!inProgress.value || !(await confirmX(t('confirm_submit_version')))) return
  await enableAfter(submitting, async () => {
    try {
      emit('updated', await service.submitVersion(props.draft.id, inProgress.value!.id))
      notifyS(t('submitted_ok'))
    } catch (err) {
      notifyError(err)
    }
  })
}

async function deleteVersion() {
  if (!inProgress.value || !(await confirmX(t('confirm_delete_version')))) return
  try {
    emit('updated', await service.deleteVersion(props.draft.id, inProgress.value.id))
  } catch (err) {
    notifyError(err)
  }
}
</script>

<template>
  <div class="step">
    <div class="review">
      <div class="review__summary gm-card">
        <div class="review__head">
          <img v-if="draft.iconUrl" :src="assetUrl(draft.iconUrl)" alt="" />
          <div>
            <h2>{{ draft.name }}</h2>
            <div class="gm-muted">{{ draft.shortDescription }}</div>
          </div>
        </div>
        <div class="review__kv">
          <span>{{ t('category') }}</span><b>{{ draft.categoryPath.map((c) => name(c)).join(' › ') || '—' }}</b>
          <span>{{ t('license') }}</span><b>{{ draft.licenseSpdxId }} <ElTag :type="draft.source.licenseMatches ? 'success' : 'danger'" size="small">{{ draft.source.licenseMatches ? t('license_ok') : t('license_mismatch') }}</ElTag></b>
          <span>{{ t('source_code') }}</span><b>{{ draft.repoUrl || (draft.source.analyzedAt ? t('source_archive') : '—') }}</b>
          <span>{{ t('platform') }}</span><b>{{ (draft.draftVersion ?? draft.versions[0])?.files.filter((f) => f.kind !== 'source').map((f) => f.platformName).join(', ') || '—' }}</b>
          <span>{{ t('screenshots') }}</span><b>{{ draft.screenshots.length }}</b>
          <span>{{ t('prompts_title') }}</span><b>{{ draft.prompts.length }}</b>
          <span>{{ t('tags') }}</span><b>{{ draft.tags.join(', ') || '—' }}</b>
          <span>{{ t('cost_title') }}</span><b>{{ formatNumber(draft.estGenerationTokens) }} {{ t('tokens') }} ≈ {{ formatMoney(draft.estGenerationCostUsd) }} <small class="gm-muted">{{ draft.estIsOverride ? t('cost_from_uploader') : t('cost_estimated') }}</small></b>
        </div>
      </div>

      <div class="review__check">
        <template v-if="!isPublished">
          <h3>{{ t('readiness') }}</h3>
          <ElResult v-if="!blocking.length && draft.status === 'draft'" icon="success" :title="t('ready_to_submit')" :sub-title="t('ready_to_submit_text')" />
          <ul v-if="blocking.length" class="issues issues--blocking">
            <li v-for="i in blocking" :key="i.code"><ElIcon><CircleCloseFilled /></ElIcon><span>{{ i.message }}</span><a class="gm-link" @click="emit('go', stepIndex[i.step] ?? 0)">{{ t('fix') }}</a></li>
          </ul>
          <ul v-if="warnings.length" class="issues">
            <li v-for="i in warnings" :key="i.code"><ElIcon><WarningFilled /></ElIcon><span>{{ i.message }}</span><a class="gm-link" @click="emit('go', stepIndex[i.step] ?? 0)">{{ t('fix') }}</a></li>
          </ul>
          <div v-if="draft.status === 'draft' || draft.status === 'rejected'" class="review__submit">
            <p class="gm-muted">{{ t('submit_hint') }}</p>
            <ElButton type="primary" size="large" :disabled="!draft.readiness.canSubmit" @click="emit('submit')">{{ t('submit_for_review') }}</ElButton>
          </div>
          <ElAlert v-else type="info" :closable="false" show-icon><template #title><StatusTag :value="draft.status" /> {{ draft.status === 'pendingScan' ? t('scan_in_progress') : t('review_in_progress') }}</template></ElAlert>
        </template>

        <template v-else>
          <h3>{{ t('versions') }}</h3>
          <div v-if="inProgress" class="gm-card review__inprogress">
            <div><strong>v{{ inProgress.version }}</strong> <StatusTag :value="inProgress.status" /> <span v-if="inProgress.rejectionReason" class="review__reason">{{ inProgress.rejectionReason }}</span></div>
            <ul v-if="blocking.length" class="issues issues--blocking"><li v-for="i in blocking" :key="i.code"><ElIcon><CircleCloseFilled /></ElIcon><span>{{ i.message }}</span><a class="gm-link" @click="emit('go', stepIndex[i.step] ?? 0)">{{ t('fix') }}</a></li></ul>
            <div class="review__inprogress-actions">
              <ElButton size="small" @click="emit('go', 2)">{{ t('edit_files') }}</ElButton>
              <ElButton v-if="inProgress.status === 'draft' || inProgress.status === 'rejected'" size="small" type="primary" :loading="submitting" :disabled="blocking.some((b) => b.step === 'files' || b.step === 'source')" @click="submitVersion">{{ t('submit_version') }}</ElButton>
              <ElButton v-if="inProgress.status === 'draft' || inProgress.status === 'rejected'" size="small" text type="danger" @click="deleteVersion">{{ t('delete') }}</ElButton>
            </div>
          </div>
          <ElButton v-else type="primary" @click="newVersion = true"><ElIcon><Plus /></ElIcon>{{ t('new_version') }}</ElButton>
          <VersionsList :versions="draft.versions" show-status style="margin-top: 14px" />
        </template>
      </div>
    </div>

    <ElDialog v-model="newVersion" :title="t('new_version')" width="520px">
      <ElForm label-position="top">
        <ElFormItem :label="t('version')"><ElInput v-model.trim="vform.version" placeholder="1.1.0" /></ElFormItem>
        <ElFormItem v-if="draft.sourceKind === 'repository'" :label="t('source_ref')"><ElInput v-model.trim="vform.sourceRef" placeholder="v1.1.0" /><div class="sub">{{ t('source_ref_hint') }}</div></ElFormItem>
        <ElFormItem :label="t('changelog')"><ElInput v-model="vform.changelog" type="textarea" :rows="4" /></ElFormItem>
      </ElForm>
      <template #footer><ElButton @click="newVersion = false">{{ t('cancel') }}</ElButton><ElButton type="primary" :loading="savingVersion" :disabled="!vform.version" @click="createVersion">{{ t('create') }}</ElButton></template>
    </ElDialog>
  </div>
</template>

<style scoped lang="scss">
.review {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 18px;
  &__summary { padding: 16px 18px; }
  &__head { display: flex; gap: 12px; align-items: center; margin-bottom: 12px; img { width: 56px; height: 56px; border-radius: 12px; } h2 { margin: 0; font-size: 20px; } }
  &__kv { display: grid; grid-template-columns: auto 1fr; gap: 6px 14px; font-size: 13px; span { color: var(--gm-text-muted); } b { font-weight: 600; word-break: break-word; } }
  &__check h3 { margin: 0 0 10px; }
  &__submit { margin-top: 14px; text-align: center; }
  &__inprogress { padding: 12px 14px; margin-bottom: 10px; &-actions { display: flex; gap: 8px; margin-top: 8px; .el-button + .el-button { margin-left: 0; } } }
  &__reason { color: var(--el-color-danger); font-size: 13px; }
  @media (max-width: 860px) { grid-template-columns: 1fr; }
}
.issues { list-style: none; padding: 0; margin: 0 0 10px; li { display: flex; gap: 8px; align-items: center; padding: 6px 8px; border-radius: 6px; margin-bottom: 4px; background: #fff8e6; color: #7a5200; html.dark & { background: #3a2e10; color: #ffd580; } span { flex: 1; } } &--blocking li { background: #fde3e3; color: #b42318; html.dark & { background: #3a1616; color: #ff9d9d; } } }
</style>
