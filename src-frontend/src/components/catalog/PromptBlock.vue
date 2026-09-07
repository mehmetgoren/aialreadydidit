<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { AppPromptDto } from '@/utils/models/catalog-models'
import { copyText } from '@/utils/tools'

/** The original prompt(s) with copy + "make my own variant" helpers. */
const props = defineProps<{ prompts: AppPromptDto[]; appName: string; repoUrl?: string | null }>()
const { t } = useI18n()

function variantPrompt(p: AppPromptDto) {
  const source = props.repoUrl ? `Start from the existing open-source implementation at ${props.repoUrl}.` : `Start from the existing open-source implementation of "${props.appName}" (download its source snapshot from the store).`
  return `${source} Do not rewrite it from scratch — read it first, keep what works and change only what is needed.\n\nOriginal prompt that produced it:\n"""\n${p.promptText}\n"""\n\nMy changes:\n- `
}
</script>

<template>
  <div class="prompts">
    <div v-for="p in prompts" :key="p.id" class="prompts__item">
      <div class="prompts__head">
        <strong>{{ p.title }}</strong>
        <span class="prompts__actions">
          <ElButton size="small" text @click="copyText(p.promptText)"><ElIcon><CopyDocument /></ElIcon>{{ t('copy_prompt') }}</ElButton>
          <ElButton size="small" text type="primary" @click="copyText(variantPrompt(p))"><ElIcon><MagicStick /></ElIcon>{{ t('make_variant') }}</ElButton>
        </span>
      </div>
      <pre class="prompts__text">{{ p.promptText }}</pre>
    </div>
  </div>
</template>

<style scoped lang="scss">
.prompts {
  display: flex;
  flex-direction: column;
  gap: 12px;
  &__item {
    border: 1px solid var(--gm-border);
    border-radius: 10px;
    overflow: hidden;
  }
  &__head {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 8px;
    padding: 8px 12px;
    background: var(--gm-page-bg);
    flex-wrap: wrap;
  }
  &__text {
    margin: 0;
    padding: 12px;
    white-space: pre-wrap;
    font-family: inherit;
    font-size: 13px;
    line-height: 1.55;
    max-height: 320px;
    overflow: auto;
  }
}
</style>
