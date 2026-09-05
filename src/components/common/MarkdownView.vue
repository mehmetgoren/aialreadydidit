<script setup lang="ts">
import { computed } from 'vue'
import { marked } from 'marked'
import DOMPurify from 'dompurify'

/** Sanitised Markdown → HTML (descriptions, READMEs, changelogs). */
const props = withDefaults(defineProps<{ source: string | null | undefined; inline?: boolean }>(), { inline: false })
const html = computed(() => {
  if (!props.source) return ''
  const raw = props.inline ? marked.parseInline(props.source) : marked.parse(props.source, { gfm: true, breaks: false })
  return DOMPurify.sanitize(String(raw), { ADD_ATTR: ['target'] })
})
</script>

<template>
  <div class="gm-markdown" v-html="html" />
</template>
