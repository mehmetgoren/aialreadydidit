<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { AppDraft, RepositoryInspection } from '@/utils/models/apps-models'
import type { AppFileDto } from '@/utils/models/catalog-models'
import { MyAppsService } from '@/utils/services/my-apps-service'
import { useSiteStore } from '@/stores/site-store'
import StatusTag from '@/components/common/StatusTag.vue'
import { confirmX, enableAfter, notifyError, notifyS, platformIcon } from '@/utils/tools'
import { formatBytes } from '@/utils/format'

/** Step 3 — ready-to-run install files (or references) per declared platform (several per platform are fine, e.g. .deb + .AppImage), plus version info. */
const props = defineProps<{ draft: AppDraft }>()
const emit = defineEmits<{ updated: [d: AppDraft]; refresh: []; next: []; back: [] }>()
const { t } = useI18n()
const site = useSiteStore()
const service = new MyAppsService()

const version = computed(() => props.draft.draftVersion)
const editable = computed(() => version.value?.status === 'draft' || version.value?.status === 'rejected')
const installers = computed(() => version.value?.files.filter((f) => f.kind !== 'source') ?? [])
const platform = ref(site.platforms[0]?.code ?? 'linux')
const platformDef = computed(() => site.platforms.find((p) => p.code === platform.value))
const installHint = ref('')
const reference = ref('')
const uploading = ref(false)
const progress = ref(0)
const fileInput = ref<HTMLInputElement | null>(null)
const versionForm = reactive({ version: version.value?.version ?? '1.0.0', changelog: version.value?.changelog ?? '', releasedAt: version.value?.releasedAt ?? null as string | null })
const savingVersion = ref(false)
const inspection = ref<RepositoryInspection | null>(null)
const loadingReleases = ref(false)
const importing = ref<string | null>(null)

async function onFile(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file || !version.value) return
  await enableAfter(uploading, async () => {
    try {
      await service.uploadInstaller(props.draft.id, version.value!.id, platform.value, file, installHint.value || undefined, (p) => (progress.value = p))
      notifyS(t('file_uploaded'))
      emit('refresh')
    } catch (err) {
      notifyError(err)
    } finally {
      if (fileInput.value) fileInput.value.value = ''
    }
  })
}

async function addReference() {
  if (!version.value) return
  try {
    await service.addExternalFile(props.draft.id, version.value.id, { platformCode: platform.value, reference: reference.value, installHint: installHint.value || null })
    reference.value = ''
    notifyS(t('saved'))
    emit('refresh')
  } catch (err) {
    notifyError(err)
  }
}

async function removeFile(f: AppFileDto) {
  if (!(await confirmX(t('confirm_remove_file', { name: f.fileName })))) return
  try {
    await service.deleteFile(props.draft.id, f.id)
    emit('refresh')
  } catch (err) {
    notifyError(err)
  }
}

async function saveVersion() {
  if (!version.value) return
  await enableAfter(savingVersion, async () => {
    try {
      emit('updated', await service.updateVersion(props.draft.id, version.value!.id, { version: versionForm.version, changelog: versionForm.changelog || null, releasedAt: versionForm.releasedAt }))
      notifyS(t('saved'))
    } catch (err) {
      notifyError(err)
    }
  })
}

async function loadReleases() {
  if (!props.draft.repoUrl) return
  await enableAfter(loadingReleases, async () => {
    try {
      inspection.value = await service.inspectRepository(props.draft.repoUrl!)
    } catch (err) {
      notifyError(err)
    }
  })
}

async function importAsset(url: string, name: string, suggested: string | null) {
  if (!version.value) return
  importing.value = url
  try {
    await service.importReleaseAsset(props.draft.id, version.value.id, { url, platformCode: suggested ?? platform.value })
    notifyS(t('file_uploaded'))
    emit('refresh')
  } catch (err) {
    notifyError(err)
  } finally {
    importing.value = null
  }
}
</script>

<template>
  <div class="step">
    <p class="step__intro">{{ t('files_intro') }}</p>
    <ElAlert type="info" :closable="false" show-icon :title="t('rule_installable')" :description="t('files_rule_hint')" style="margin-bottom: 16px" />

    <div v-if="version" class="gm-card step__box">
      <h3>{{ t('version_info') }}</h3>
      <ElForm label-position="top" class="grid-form">
        <ElFormItem :label="t('version')"><ElInput v-model.trim="versionForm.version" :disabled="!editable" placeholder="1.0.0" /></ElFormItem>
        <ElFormItem :label="t('release_date')"><ElDatePicker v-model="versionForm.releasedAt" type="date" :disabled="!editable" style="width: 100%" /></ElFormItem>
        <ElFormItem :label="t('changelog')" class="full"><ElInput v-model="versionForm.changelog" type="textarea" :rows="3" :placeholder="t('changelog_hint')" /></ElFormItem>
      </ElForm>
      <ElButton :loading="savingVersion" @click="saveVersion">{{ t('save') }}</ElButton>
    </div>

    <h3 class="step__h3">{{ t('install_files') }}</h3>
    <ElTable :data="installers" class="gm-table" size="small">
      <ElTableColumn :label="t('platform')" width="150"><template #default="{ row }">{{ platformIcon(row.platformCode) }} {{ site.platformName(row.platformCode) }}</template></ElTableColumn>
      <ElTableColumn :label="t('file')" min-width="220"><template #default="{ row }"><span style="word-break: break-all">{{ row.fileName }}</span><div v-if="row.installHint" class="sub">{{ row.installHint }}</div></template></ElTableColumn>
      <ElTableColumn :label="t('size')" width="100"><template #default="{ row }">{{ row.externalReference ? '—' : formatBytes(row.sizeBytes) }}</template></ElTableColumn>
      <ElTableColumn :label="t('scan')" width="110"><template #default="{ row }"><StatusTag :value="row.scanStatus" /></template></ElTableColumn>
      <ElTableColumn width="90" align="right"><template #default="{ row }"><ElButton v-if="editable" size="small" text type="danger" @click="removeFile(row as AppFileDto)">{{ t('remove') }}</ElButton></template></ElTableColumn>
      <template #empty><span class="gm-muted">{{ t('no_install_files') }}</span></template>
    </ElTable>

    <div v-if="editable" class="gm-card step__box" style="margin-top: 14px">
      <h3>{{ t('add_install_file') }}</h3>
      <div class="add-row">
        <ElSelect v-model="platform" style="width: 200px">
          <ElOption v-for="p in site.platforms" :key="p.code" :value="p.code" :label="`${platformIcon(p.code)} ${p.name}`" />
        </ElSelect>
        <ElInput v-model="installHint" :placeholder="t('install_hint_placeholder')" style="flex: 1" />
      </div>
      <div class="sub" style="margin: 6px 0 10px">{{ t('accepted_extensions') }}: {{ platformDef?.allowedExtensions }} · {{ t('max_size') }} {{ formatBytes(site.limits?.maxInstallerBytes) }}</div>
      <div class="add-row">
        <input ref="fileInput" type="file" hidden @change="onFile" />
        <ElButton type="primary" :loading="uploading" @click="fileInput?.click()"><ElIcon><Upload /></ElIcon>{{ t('upload_file') }}</ElButton>
        <template v-if="platformDef?.allowsExternalReference">
          <span class="gm-muted">{{ t('or') }}</span>
          <ElInput v-model.trim="reference" :placeholder="platform === 'docker' ? 'ghcr.io/user/app:1.0' : 'https://app.example.com'" style="flex: 1" />
          <ElButton :disabled="!reference" @click="addReference">{{ t('add_reference') }}</ElButton>
        </template>
      </div>
      <ElProgress v-if="uploading" :percentage="progress" style="margin-top: 10px" />

      <template v-if="draft.repoUrl">
        <ElDivider />
        <ElButton size="small" :loading="loadingReleases" @click="loadReleases">{{ t('import_from_releases') }}</ElButton>
        <div v-if="inspection" class="releases">
          <div v-for="r in inspection.releases.filter((x) => x.assets.length)" :key="r.tag" class="releases__item">
            <strong>{{ r.tag }}</strong> <span class="gm-muted">{{ r.name }}</span>
            <div v-for="a in r.assets" :key="a.url" class="releases__asset">
              <span>{{ platformIcon(a.suggestedPlatform) }} {{ a.name }} <small class="gm-muted">{{ formatBytes(a.size) }}</small></span>
              <ElButton size="small" :loading="importing === a.url" @click="importAsset(a.url, a.name, a.suggestedPlatform)">{{ t('import') }} → {{ site.platformName(a.suggestedPlatform ?? platform) }}</ElButton>
            </div>
          </div>
          <div v-if="!inspection.releases.some((x) => x.assets.length)" class="gm-muted">{{ t('no_release_assets') }}</div>
        </div>
      </template>
    </div>

    <div class="step__nav">
      <ElButton @click="emit('back')">← {{ t('back') }}</ElButton>
      <ElButton type="primary" @click="emit('next')">{{ t('next') }} →</ElButton>
    </div>
  </div>
</template>

<style scoped lang="scss">
.step {
  &__intro { color: var(--gm-text-muted); margin: 0 0 12px; }
  &__box { padding: 14px 16px; h3 { margin: 0 0 10px; font-size: 15px; } }
  &__h3 { margin: 20px 0 8px; }
  &__nav { display: flex; justify-content: space-between; margin-top: 24px; padding-top: 16px; border-top: 1px solid var(--gm-border); }
}
.add-row { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
.releases { margin-top: 10px; &__item { padding: 8px 0; border-top: 1px solid var(--gm-border); } &__asset { display: flex; justify-content: space-between; align-items: center; gap: 8px; padding: 4px 0 4px 12px; font-size: 13px; } }
</style>
