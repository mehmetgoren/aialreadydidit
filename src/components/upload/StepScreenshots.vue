<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { AppDraft } from '@/utils/models/apps-models'
import type { ScreenshotDto } from '@/utils/models/catalog-models'
import { MyAppsService } from '@/utils/services/my-apps-service'
import { useSiteStore } from '@/stores/site-store'
import { assetUrl, confirmX, notifyError, notifyS } from '@/utils/tools'
import { formatBytes } from '@/utils/format'

/** Step 4 — screenshots (min / recommended from site settings) + optional icon. */
const props = defineProps<{ draft: AppDraft }>()
const emit = defineEmits<{ updated: [d: AppDraft]; refresh: []; next: []; back: [] }>()
const { t } = useI18n()
const site = useSiteStore()
const service = new MyAppsService()
const fileInput = ref<HTMLInputElement | null>(null)
const iconInput = ref<HTMLInputElement | null>(null)
const uploading = ref(false)
const progress = ref('')
const limits = computed(() => site.limits)
const count = computed(() => props.draft.screenshots.length)

async function onFiles(e: Event) {
  const files = Array.from((e.target as HTMLInputElement).files ?? [])
  if (!files.length) return
  uploading.value = true
  try {
    let i = 0
    for (const file of files) {
      progress.value = `${++i}/${files.length}`
      await service.uploadScreenshot(props.draft.id, file)
    }
    notifyS(t('screenshots_uploaded'))
    emit('refresh')
  } catch (err) {
    notifyError(err)
    emit('refresh')
  } finally {
    uploading.value = false
    if (fileInput.value) fileInput.value.value = ''
  }
}

async function remove(s: ScreenshotDto) {
  if (!(await confirmX(t('confirm_remove_screenshot')))) return
  try {
    await service.deleteScreenshot(props.draft.id, s.id)
    emit('refresh')
  } catch (err) {
    notifyError(err)
  }
}

async function move(index: number, dir: -1 | 1) {
  const ids = props.draft.screenshots.map((s) => s.id)
  const target = index + dir
  if (target < 0 || target >= ids.length) return
  ;[ids[index], ids[target]] = [ids[target]!, ids[index]!]
  try {
    await service.reorderScreenshots(props.draft.id, ids)
    emit('refresh')
  } catch (err) {
    notifyError(err)
  }
}

async function caption(s: ScreenshotDto, value: string) {
  try {
    await service.setCaption(props.draft.id, s.id, value || null)
  } catch (err) {
    notifyError(err)
  }
}

async function onIcon(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file) return
  try {
    await service.uploadIcon(props.draft.id, file)
    emit('refresh')
  } catch (err) {
    notifyError(err)
  } finally {
    if (iconInput.value) iconInput.value.value = ''
  }
}
async function removeIcon() {
  try {
    await service.deleteIcon(props.draft.id)
    emit('refresh')
  } catch (err) {
    notifyError(err)
  }
}
</script>

<template>
  <div class="step">
    <p class="step__intro">{{ t('screenshots_intro', { min: limits?.minScreenshots ?? 1, recommended: limits?.recommendedScreenshots ?? 3, max: limits?.maxScreenshots ?? 10, size: formatBytes(limits?.maxScreenshotBytes) }) }}</p>
    <div class="shots">
      <div v-for="(s, i) in draft.screenshots" :key="s.id" class="shot">
        <img :src="assetUrl(s.thumbUrl)" :alt="s.caption || ''" />
        <ElInput :model-value="s.caption ?? ''" size="small" :placeholder="t('caption')" maxlength="200" @change="caption(s, $event)" />
        <div class="shot__actions">
          <ElButton size="small" text :disabled="i === 0" @click="move(i, -1)">←</ElButton>
          <ElButton size="small" text :disabled="i === draft.screenshots.length - 1" @click="move(i, 1)">→</ElButton>
          <ElButton size="small" text type="danger" @click="remove(s)">{{ t('remove') }}</ElButton>
        </div>
      </div>
      <button v-if="count < (limits?.maxScreenshots ?? 10)" type="button" class="shot shot--add" :disabled="uploading" @click="fileInput?.click()">
        <ElIcon :size="28"><Plus /></ElIcon>
        <span>{{ uploading ? progress : t('add_screenshots') }}</span>
      </button>
      <input ref="fileInput" type="file" accept="image/png,image/jpeg,image/webp,image/gif" multiple hidden @change="onFiles" />
    </div>
    <div class="sub">{{ t('screenshots_count', { n: count }) }}</div>

    <h3 class="step__h3">{{ t('icon') }}</h3>
    <div class="icon-row">
      <img v-if="draft.iconUrl" :src="assetUrl(draft.iconUrl)" class="icon-row__img" alt="" />
      <div v-else class="icon-row__img icon-row__img--empty"><ElIcon><Picture /></ElIcon></div>
      <input ref="iconInput" type="file" accept="image/png,image/jpeg,image/webp" hidden @change="onIcon" />
      <ElButton @click="iconInput?.click()">{{ draft.iconUrl ? t('replace_icon') : t('upload_icon') }}</ElButton>
      <ElButton v-if="draft.iconUrl" text type="danger" @click="removeIcon">{{ t('remove') }}</ElButton>
      <span class="sub">{{ t('icon_hint') }}</span>
    </div>

    <div class="step__nav">
      <ElButton @click="emit('back')">← {{ t('back') }}</ElButton>
      <ElButton type="primary" @click="emit('next')">{{ t('next') }} →</ElButton>
    </div>
  </div>
</template>

<style scoped lang="scss">
.step__intro { color: var(--gm-text-muted); margin: 0 0 12px; }
.step__h3 { margin: 22px 0 8px; }
.shots { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 12px; }
.shot {
  border: 1px solid var(--gm-border); border-radius: 10px; padding: 8px; display: flex; flex-direction: column; gap: 6px; background: var(--gm-card-bg);
  img { width: 100%; aspect-ratio: 16/10; object-fit: cover; border-radius: 6px; }
  &__actions { display: flex; justify-content: space-between; }
  &--add { align-items: center; justify-content: center; min-height: 170px; cursor: pointer; border-style: dashed; color: var(--gm-primary); font-weight: 600; gap: 8px; &:hover { background: var(--gm-primary-light); } }
}
.icon-row { display: flex; align-items: center; gap: 12px; &__img { width: 64px; height: 64px; border-radius: 14px; object-fit: cover; &--empty { display: grid; place-items: center; background: var(--gm-page-bg); border: 1px dashed var(--gm-border); color: var(--gm-text-muted); } } }
.step__nav { display: flex; justify-content: space-between; margin-top: 24px; padding-top: 16px; border-top: 1px solid var(--gm-border); }
</style>
