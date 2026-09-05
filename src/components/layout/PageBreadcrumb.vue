<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useCommonStore } from '@/stores/common-store'

const route = useRoute()
const { t } = useI18n()
const common = useCommonStore()

const items = computed(() => {
  if (route.path === '/') return []
  if (common.breadcrumb.length) return common.breadcrumb
  const meta = route.meta.breadcrumb
  if (meta) return meta.map((m) => ({ label: t(m.labelKey), to: m.to }))
  if (route.meta.titleKey) return [{ label: t(route.meta.titleKey) }]
  return []
})
</script>

<template>
  <div v-if="items.length" class="gm-container breadcrumb">
    <RouterLink to="/">{{ t('home') }}</RouterLink>
    <template v-for="(item, i) in items" :key="i">
      <ElIcon class="breadcrumb__sep"><ArrowRight /></ElIcon>
      <RouterLink v-if="item.to && i < items.length - 1" :to="item.to">{{ item.label }}</RouterLink>
      <span v-else>{{ item.label }}</span>
    </template>
  </div>
</template>

<style scoped>
.breadcrumb {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px;
  font-size: 13px;
  padding: 14px 16px 6px;
  color: var(--gm-text);
}
.breadcrumb a {
  color: var(--gm-text-muted);
}
.breadcrumb__sep {
  color: var(--gm-text-muted);
  font-size: 11px;
}
</style>
