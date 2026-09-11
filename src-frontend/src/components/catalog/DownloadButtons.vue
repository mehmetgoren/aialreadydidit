<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { AppFileDto } from '@/utils/models/catalog-models'
import { AppsService } from '@/utils/services/apps-service'
import { useSiteStore } from '@/stores/site-store'
import { formatBytes } from '@/utils/format'
import { copyText, notifyError, platformIcon } from '@/utils/tools'

/** One button per install file of the latest version (a platform may have several, e.g. .deb and .AppImage) + the source snapshot. Asks the API for a fresh link (counts the download). */
const props = defineProps<{ slug: string; files: AppFileDto[]; version: string }>()
const emit = defineEmits<{ downloaded: [] }>()
const { t } = useI18n()
const site = useSiteStore()
const busy = ref<number | null>(null)
const hint = ref<{ file: AppFileDto; hint: string | null; sha: string | null } | null>(null)

/** Platforms that carry more than one file get the extension in the button label so the two "Download for Linux" buttons differ. */
const perPlatform = computed(() => {
  const counts = new Map<string | null, number>()
  for (const f of props.files) if (f.kind !== 'source') counts.set(f.platformCode, (counts.get(f.platformCode) ?? 0) + 1)
  return counts
})
function extensionOf(file: AppFileDto): string {
  const m = /\.(tar\.gz|tar\.xz|[a-z0-9]+)$/i.exec(file.fileName)
  return m ? `.${m[1]}` : ''
}
function label(file: AppFileDto): string {
  const base = t('download_for', { platform: site.platformName(file.platformCode) })
  const ext = extensionOf(file)
  return (perPlatform.value.get(file.platformCode) ?? 0) > 1 && ext ? `${base} (${ext})` : base
}

async function download(file: AppFileDto) {
  busy.value = file.id
  try {
    const link = await new AppsService().downloadLink(props.slug, file.id)
    if (file.externalReference) {
      if (file.kind === 'webBundle' && /^https?:/.test(file.externalReference)) window.open(file.externalReference, '_blank', 'noopener')
      else await copyText(file.externalReference)
    } else {
      const a = document.createElement('a')
      a.href = link.url
      a.rel = 'noopener'
      document.body.appendChild(a)
      a.click()
      a.remove()
    }
    hint.value = { file, hint: link.installHint, sha: link.sha256 }
    emit('downloaded')
  } catch (err) {
    notifyError(err)
  } finally {
    busy.value = null
  }
}
</script>

<template>
  <div class="dl">
    <div class="dl__buttons">
      <ElButton
        v-for="f in files.filter((x) => x.kind !== 'source')"
        :key="f.id"
        type="primary"
        size="large"
        :loading="busy === f.id"
        :disabled="f.scanStatus === 'infected' || f.scanStatus === 'pending'"
        class="dl__btn"
        @click="download(f)"
      >
        <span class="dl__icon">{{ platformIcon(f.platformCode) }}</span>
        <span class="dl__label">
          <span>{{ f.kind === 'dockerImage' ? t('copy_docker_ref') : f.kind === 'webBundle' && f.externalReference ? t('open_web_app') : label(f) }}</span>
          <small>{{ f.externalReference ? f.externalReference : `${f.fileName} · ${formatBytes(f.sizeBytes)}` }}</small>
        </span>
      </ElButton>
      <ElButton v-for="f in files.filter((x) => x.kind === 'source')" :key="f.id" size="large" :loading="busy === f.id" class="dl__btn dl__btn--source" @click="download(f)">
        <span class="dl__icon">📦</span>
        <span class="dl__label"><span>{{ t('download_source') }}</span><small>{{ f.fileName }} · {{ formatBytes(f.sizeBytes) }}</small></span>
      </ElButton>
    </div>
    <div class="dl__version gm-muted">{{ t('version') }} {{ version }}</div>
    <ElAlert v-if="hint" type="success" :closable="true" show-icon class="dl__hint" @close="hint = null">
      <template #title>{{ t('download_started', { file: hint.file.fileName }) }}</template>
      <div v-if="hint.hint" class="dl__hint-text"><code>{{ hint.hint }}</code></div>
      <div v-if="hint.sha" class="dl__hint-text">sha256: <code>{{ hint.sha }}</code></div>
    </ElAlert>
  </div>
</template>

<style scoped lang="scss">
.dl {
  &__buttons {
    display: flex;
    flex-direction: column;
    gap: 8px;
    .el-button + .el-button {
      margin-left: 0;
    }
  }
  &__btn {
    justify-content: flex-start;
    height: auto;
    padding: 10px 14px;
    width: 100%;
    &--source {
      --el-button-bg-color: var(--gm-card-bg);
    }
  }
  &__icon {
    font-size: 22px;
    margin-right: 10px;
  }
  &__label {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    line-height: 1.2;
    text-align: left;
    min-width: 0;
    small {
      font-weight: 400;
      opacity: 0.8;
      font-size: 11px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      max-width: 260px;
    }
  }
  &__version {
    font-size: 12px;
    margin-top: 6px;
  }
  &__hint {
    margin-top: 10px;
    &-text {
      font-size: 12px;
      margin-top: 4px;
      word-break: break-all;
    }
  }
}
</style>
