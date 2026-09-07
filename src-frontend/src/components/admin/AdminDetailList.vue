<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { formatDate, formatDateTime, formatMoney, formatNumber } from '@/utils/format'

export interface DetailItem {
  label: string
  value?: string | number | boolean | null
  type?: 'money' | 'date' | 'datetime' | 'bool' | 'number' | 'text'
  href?: string
  to?: string
}

withDefaults(defineProps<{ items: DetailItem[]; columns?: number; title?: string; border?: boolean }>(), { columns: 2, title: '', border: true })
const { t } = useI18n()

function render(i: DetailItem): string {
  if (i.value === null || i.value === undefined || i.value === '') return '—'
  switch (i.type) {
    case 'money':
      return formatMoney(Number(i.value))
    case 'number':
      return formatNumber(Number(i.value))
    case 'date':
      return formatDate(String(i.value))
    case 'datetime':
      return formatDateTime(String(i.value))
    case 'bool':
      return i.value ? t('yes') : t('no')
    default:
      return String(i.value)
  }
}
</script>

<template>
  <ElDescriptions :title="title" :column="columns" :border="border" size="small" class="detail-list">
    <ElDescriptionsItem v-for="(i, idx) in items" :key="idx" :label="i.label">
      <a v-if="i.href" :href="i.href" class="gm-link" target="_blank" rel="noopener">{{ render(i) }}</a>
      <RouterLink v-else-if="i.to" :to="i.to" class="gm-link">{{ render(i) }}</RouterLink>
      <template v-else>{{ render(i) }}</template>
    </ElDescriptionsItem>
  </ElDescriptions>
</template>

<style scoped>
.detail-list :deep(.el-descriptions__label) {
  width: 180px;
  color: var(--gm-text-muted);
}
</style>
