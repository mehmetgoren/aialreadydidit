<script setup lang="ts">
import { computed } from 'vue'

/** Small colored tag driven by a status code → tone map. */
const props = withDefaults(
  defineProps<{
    value: string | number | boolean | null | undefined
    label?: string
    map?: Record<string, 'success' | 'warning' | 'danger' | 'info' | 'primary'>
  }>(),
  {
    label: '',
    map: () => ({}),
  },
)
const type = computed(() => {
  if (typeof props.value === 'boolean') return props.value ? 'success' : 'info'
  return props.map[String(props.value)] ?? 'info'
})
</script>

<template>
  <ElTag :type="type" size="small" effect="light" round>{{ label || String(value ?? '—') }}</ElTag>
</template>
