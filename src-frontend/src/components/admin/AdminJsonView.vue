<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{ value: unknown }>()
const text = computed(() => {
  const v = props.value
  if (v === null || v === undefined) return ''
  if (typeof v === 'string') {
    try {
      return JSON.stringify(JSON.parse(v), null, 2)
    } catch {
      return v
    }
  }
  return JSON.stringify(v, null, 2)
})
</script>

<template>
  <pre class="json-view">{{ text || '—' }}</pre>
</template>

<style scoped>
.json-view {
  margin: 0;
  padding: 10px 12px;
  background: var(--gm-page-bg);
  border: 1px solid var(--gm-border);
  border-radius: 6px;
  font-size: 12px;
  line-height: 1.5;
  max-height: 360px;
  overflow: auto;
  white-space: pre-wrap;
  word-break: break-word;
}
</style>
