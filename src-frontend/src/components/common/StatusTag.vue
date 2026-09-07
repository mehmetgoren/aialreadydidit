<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

/** App / version / scan status → coloured tag with the i18n label (status_<value>). */
const props = defineProps<{ value: string | null | undefined; prefix?: string }>()
const { t } = useI18n()
const map: Record<string, 'success' | 'warning' | 'danger' | 'info' | 'primary'> = {
  published: 'success',
  clean: 'success',
  done: 'success',
  open: 'warning',
  pendingScan: 'warning',
  pendingReview: 'warning',
  pending: 'warning',
  queued: 'info',
  running: 'primary',
  draft: 'info',
  skipped: 'info',
  unlisted: 'info',
  rejected: 'danger',
  infected: 'danger',
  failed: 'danger',
  removed: 'danger',
  error: 'danger',
  fulfilled: 'success',
  closed: 'info',
  resolved: 'success',
  dismissed: 'info',
  reviewing: 'primary',
}
const type = computed(() => map[String(props.value)] ?? 'info')
const label = computed(() => {
  const key = `${props.prefix ?? 'status'}_${String(props.value)}`
  const translated = t(key)
  return translated === key ? String(props.value ?? '—') : translated
})
</script>

<template>
  <ElTag :type="type" size="small" effect="light" round>{{ label }}</ElTag>
</template>
