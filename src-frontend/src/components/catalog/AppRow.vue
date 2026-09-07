<script setup lang="ts">
import type { AppCardDto } from '@/utils/models/catalog-models'
import AppCard from './AppCard.vue'

/** Horizontal scroll row of cards (home page sections). */
defineProps<{ title: string; apps: AppCardDto[]; to?: string; subtitle?: string }>()
</script>

<template>
  <section v-if="apps.length" class="gm-section app-row">
    <div class="gm-section__head">
      <div>
        <h2>{{ title }}</h2>
        <div v-if="subtitle" class="sub">{{ subtitle }}</div>
      </div>
      <RouterLink v-if="to" :to="to" class="gm-link">{{ $t('see_all') }} →</RouterLink>
    </div>
    <div class="app-row__scroll">
      <AppCard v-for="app in apps" :key="app.id" :app="app" class="app-row__card" />
    </div>
  </section>
</template>

<style scoped lang="scss">
.app-row {
  &__scroll {
    display: grid;
    grid-auto-flow: column;
    grid-auto-columns: minmax(240px, 260px);
    gap: 14px;
    overflow-x: auto;
    padding-bottom: 8px;
    scroll-snap-type: x proximity;
    scrollbar-width: thin;
  }
  &__card {
    scroll-snap-align: start;
  }
}
</style>
