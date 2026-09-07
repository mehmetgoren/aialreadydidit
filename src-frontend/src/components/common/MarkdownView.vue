<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { marked } from 'marked'
import DOMPurify from 'dompurify'
import { resolveRelative } from '@/utils/markdown'

/**
 * Sanitised Markdown → HTML (descriptions, READMEs, changelogs).
 * `baseUrl` resolves relative image / link paths (README assets of a linked repository);
 * images that still fail to load are hidden instead of showing a broken icon.
 */
const props = withDefaults(defineProps<{ source: string | null | undefined; inline?: boolean; baseUrl?: string | null }>(), { inline: false, baseUrl: null })
const root = ref<HTMLElement | null>(null)

const html = computed(() => {
  if (!props.source) return ''
  const raw = props.inline ? marked.parseInline(props.source) : marked.parse(props.source, { gfm: true, breaks: false })
  const clean = DOMPurify.sanitize(String(raw), { ADD_ATTR: ['target'] })
  return props.baseUrl ? resolveRelative(clean, props.baseUrl) : clean
})

function hideBrokenImages() {
  root.value?.querySelectorAll('img').forEach((img) => {
    img.addEventListener('error', () => (img.hidden = true), { once: true })
    if (img.complete && img.naturalWidth === 0 && img.src) img.hidden = true
  })
}
watch(html, () => nextTick(hideBrokenImages), { immediate: true, flush: 'post' })
</script>

<template>
  <div ref="root" class="gm-markdown" v-html="html" />
</template>
