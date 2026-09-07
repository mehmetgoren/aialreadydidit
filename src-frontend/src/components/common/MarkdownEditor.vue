<script setup lang="ts">
import { computed, nextTick, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import MarkdownView from '@/components/common/MarkdownView.vue'

/**
 * Dependency-free Markdown editor: a formatting toolbar over a plain textarea plus a Write / Preview switch that
 * renders through the same sanitised MarkdownView the storefront uses. Toolbar actions work on the current selection
 * (or insert a placeholder when nothing is selected); Ctrl/⌘+B and Ctrl/⌘+I are wired as shortcuts.
 */
const props = withDefaults(defineProps<{ modelValue: string | null | undefined; placeholder?: string; rows?: number; maxlength?: number }>(), { placeholder: '', rows: 10, maxlength: undefined })
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()
const { t } = useI18n()

const mode = ref<'write' | 'preview'>('write')
const textarea = ref<HTMLTextAreaElement | null>(null)
const text = computed(() => props.modelValue ?? '')

type Edit = { value: string; start: number; end: number }
type Action = { key: string; icon: string; run: (value: string, start: number, end: number) => Edit }

/** Wraps the selection with `before` / `after`; inserts `fallback` (selected) when nothing is selected. */
function wrap(before: string, after: string, fallback: string): Action['run'] {
  return (value, start, end) => {
    const selected = value.slice(start, end) || fallback
    const next = value.slice(0, start) + before + selected + after + value.slice(end)
    return { value: next, start: start + before.length, end: start + before.length + selected.length }
  }
}

/** Prefixes every line touched by the selection (numbered lists count up). */
function prefixLines(prefix: string | ((index: number) => string)): Action['run'] {
  return (value, start, end) => {
    const lineStart = value.lastIndexOf('\n', start - 1) + 1
    const lineEndIndex = value.indexOf('\n', Math.max(end - 1, lineStart))
    const lineEnd = lineEndIndex === -1 ? value.length : lineEndIndex
    const lines = value.slice(lineStart, lineEnd).split('\n')
    const changed = lines.map((line, i) => (typeof prefix === 'string' ? prefix : prefix(i)) + line).join('\n')
    const next = value.slice(0, lineStart) + changed + value.slice(lineEnd)
    return { value: next, start: lineStart, end: lineStart + changed.length }
  }
}

const code: Action['run'] = (value, start, end) => {
  const selected = value.slice(start, end)
  return selected.includes('\n') ? wrap('```\n', '\n```', '')(value, start, end) : wrap('`', '`', 'code')(value, start, end)
}

const actions: Action[] = [
  { key: 'md_bold', icon: 'B', run: wrap('**', '**', 'bold') },
  { key: 'md_italic', icon: 'I', run: wrap('_', '_', 'italic') },
  { key: 'md_heading', icon: 'H', run: prefixLines('## ') },
  { key: 'md_quote', icon: '❝', run: prefixLines('> ') },
  { key: 'md_code', icon: '</>', run: code },
  { key: 'md_link', icon: '🔗', run: wrap('[', '](https://)', 'link text') },
  { key: 'md_image', icon: '🖼', run: wrap('![', '](https://)', 'alt text') },
  { key: 'md_bullets', icon: '•', run: prefixLines('- ') },
  { key: 'md_numbers', icon: '1.', run: prefixLines((i) => `${i + 1}. `) },
]

function apply(action: Action) {
  const el = textarea.value
  const start = el?.selectionStart ?? text.value.length
  const end = el?.selectionEnd ?? text.value.length
  const edit = action.run(text.value, start, end)
  emit('update:modelValue', edit.value)
  nextTick(() => {
    if (!el) return
    el.focus()
    el.setSelectionRange(edit.start, edit.end)
  })
}

function onInput(e: Event) {
  emit('update:modelValue', (e.target as HTMLTextAreaElement).value)
}

function onKeydown(e: KeyboardEvent) {
  if (!(e.ctrlKey || e.metaKey)) return
  const k = e.key.toLowerCase()
  if (k === 'b') { e.preventDefault(); apply(actions[0]!) }
  else if (k === 'i') { e.preventDefault(); apply(actions[1]!) }
}

defineExpose({ focus: () => textarea.value?.focus() })
</script>

<template>
  <div class="md-editor" :class="{ 'md-editor--preview': mode === 'preview' }">
    <div class="md-editor__bar">
      <div class="md-editor__tools" role="toolbar">
        <button v-for="a in actions" :key="a.key" type="button" class="md-editor__tool" :class="`md-editor__tool--${a.key}`" :title="t(a.key)" :aria-label="t(a.key)" :disabled="mode === 'preview'" @mousedown.prevent @click="apply(a)">{{ a.icon }}</button>
      </div>
      <div class="md-editor__modes" role="tablist">
        <button type="button" class="md-editor__mode" :class="{ 'is-active': mode === 'write' }" role="tab" :aria-selected="mode === 'write'" @click="mode = 'write'">{{ t('md_write') }}</button>
        <button type="button" class="md-editor__mode" :class="{ 'is-active': mode === 'preview' }" role="tab" :aria-selected="mode === 'preview'" @click="mode = 'preview'">{{ t('md_preview') }}</button>
      </div>
    </div>
    <textarea v-show="mode === 'write'" ref="textarea" class="md-editor__input" :value="text" :rows="rows" :placeholder="placeholder" :maxlength="maxlength" spellcheck="true" @input="onInput" @keydown="onKeydown" />
    <div v-if="mode === 'preview'" class="md-editor__preview" :style="{ minHeight: `${rows * 1.6}em` }">
      <MarkdownView v-if="text.trim()" :source="text" />
      <p v-else class="md-editor__empty">{{ t('md_preview_empty') }}</p>
    </div>
    <div class="md-editor__foot">
      <span class="md-editor__hint">{{ t('md_help') }}</span>
      <span class="md-editor__count">{{ text.length }}<template v-if="maxlength"> / {{ maxlength }}</template></span>
    </div>
  </div>
</template>

<style scoped lang="scss">
.md-editor {
  width: 100%;
  border: 1px solid var(--el-border-color);
  border-radius: var(--el-border-radius-base);
  background: var(--el-fill-color-blank);
  transition: border-color 0.2s;
  &:focus-within { border-color: var(--el-color-primary); }
  &__bar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
    flex-wrap: wrap;
    padding: 4px 6px;
    border-bottom: 1px solid var(--el-border-color-lighter);
    background: var(--el-fill-color-light);
    border-radius: var(--el-border-radius-base) var(--el-border-radius-base) 0 0;
  }
  &__tools { display: flex; flex-wrap: wrap; gap: 2px; }
  &__tool {
    min-width: 28px;
    height: 26px;
    padding: 0 6px;
    border: 0;
    border-radius: 4px;
    background: transparent;
    color: var(--el-text-color-regular);
    font: inherit;
    font-size: 13px;
    line-height: 26px;
    cursor: pointer;
    &:hover:not(:disabled) { background: var(--el-fill-color-darker); color: var(--el-text-color-primary); }
    &:disabled { opacity: 0.4; cursor: default; }
    &--md_bold { font-weight: 700; }
    &--md_italic { font-style: italic; font-family: Georgia, 'Times New Roman', serif; }
    &--md_heading { font-weight: 700; }
    &--md_code { font-family: var(--el-font-family-mono, ui-monospace, monospace); font-size: 11px; }
  }
  &__modes { display: flex; gap: 2px; }
  &__mode {
    height: 26px;
    padding: 0 10px;
    border: 0;
    border-radius: 4px;
    background: transparent;
    color: var(--el-text-color-secondary);
    font: inherit;
    font-size: 13px;
    cursor: pointer;
    &.is-active { background: var(--el-fill-color-blank); color: var(--el-color-primary); font-weight: 600; box-shadow: var(--el-box-shadow-lighter); }
  }
  &__input {
    display: block;
    width: 100%;
    padding: 8px 12px;
    border: 0;
    background: transparent;
    color: var(--el-text-color-regular);
    font: inherit;
    font-size: var(--el-font-size-base);
    line-height: 1.6;
    resize: vertical;
    outline: none;
    &::placeholder { color: var(--el-text-color-placeholder); }
  }
  &__preview { padding: 8px 12px; }
  &__empty { margin: 0; color: var(--el-text-color-placeholder); }
  &__foot {
    display: flex;
    justify-content: space-between;
    gap: 12px;
    padding: 3px 10px;
    border-top: 1px solid var(--el-border-color-lighter);
    color: var(--el-text-color-secondary);
    font-size: 12px;
  }
  &__hint { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  &__count { flex: none; }
}
</style>
