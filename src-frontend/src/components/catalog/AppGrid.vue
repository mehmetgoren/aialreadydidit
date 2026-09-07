<script setup lang="ts">
import type { AppCardDto } from '@/utils/models/catalog-models'
import AppCard from './AppCard.vue'
import EmptyState from '@/components/common/EmptyState.vue'

withDefaults(defineProps<{ apps: AppCardDto[]; loading?: boolean; emptyTitle?: string; emptyText?: string; showSimilarity?: boolean }>(), {
  loading: false,
  emptyTitle: '',
  emptyText: '',
  showSimilarity: false,
})
</script>

<template>
  <div v-loading="loading" class="app-grid-wrap">
    <div v-if="apps.length" class="gm-grid">
      <AppCard v-for="app in apps" :key="app.id" :app="app" :show-similarity="showSimilarity" />
    </div>
    <EmptyState v-else-if="!loading" :title="emptyTitle" :text="emptyText"><slot name="empty" /></EmptyState>
  </div>
</template>

<style scoped>
.app-grid-wrap {
  min-height: 120px;
}
</style>
