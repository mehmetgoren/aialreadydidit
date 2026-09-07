<script setup lang="ts">
import { useSiteStore } from '@/stores/site-store'
import { platformIcon } from '@/utils/tools'

withDefaults(defineProps<{ platforms: string[]; max?: number; size?: 'small' | 'default' }>(), { max: 8, size: 'small' })
const site = useSiteStore()
</script>

<template>
  <span class="platforms">
    <ElTooltip v-for="p in platforms.slice(0, max)" :key="p" :content="site.platformName(p)">
      <span class="platforms__badge" :class="`is-${size}`">{{ platformIcon(p) }}</span>
    </ElTooltip>
    <span v-if="platforms.length > max" class="gm-muted">+{{ platforms.length - max }}</span>
  </span>
</template>

<style scoped>
.platforms {
  display: inline-flex;
  gap: 4px;
  align-items: center;
}
.platforms__badge {
  display: inline-grid;
  place-items: center;
  width: 22px;
  height: 22px;
  border-radius: 6px;
  background: var(--gm-page-bg);
  border: 1px solid var(--gm-border);
  font-size: 12px;
}
.platforms__badge.is-default {
  width: 28px;
  height: 28px;
  font-size: 15px;
}
</style>
