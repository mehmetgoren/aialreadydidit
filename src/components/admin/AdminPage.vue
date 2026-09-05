<script setup lang="ts">
/** Shell of every admin-panel screen: title bar with a toolbar slot + loading-aware body card. */
withDefaults(defineProps<{ title: string; subtitle?: string; loading?: boolean }>(), {
  subtitle: '',
  loading: false,
})
</script>

<template>
  <div class="admin-page">
    <header class="admin-page__head">
      <div>
        <h2 class="admin-page__title">{{ title }}</h2>
        <p v-if="subtitle" class="admin-page__subtitle">{{ subtitle }}</p>
      </div>
      <div class="admin-page__actions"><slot name="actions" /></div>
    </header>
    <div v-loading="loading" class="admin-page__body">
      <slot />
    </div>
  </div>
</template>

<style scoped lang="scss">
.admin-page {
  background: var(--gm-card-bg);
  border: 1px solid var(--gm-border);
  border-radius: var(--gm-radius, 8px);
  padding: 16px 18px 18px;
  &__head {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 12px;
    margin-bottom: 14px;
    flex-wrap: wrap;
  }
  &__title {
    font-size: 17px;
    font-weight: 700;
    margin: 0;
  }
  &__subtitle {
    margin: 4px 0 0;
    font-size: 12px;
    color: var(--gm-text-muted);
  }
  &__actions {
    display: flex;
    align-items: center;
    gap: 8px;
    flex-wrap: wrap;
  }
  &__body {
    min-height: 120px;
  }
}
</style>
