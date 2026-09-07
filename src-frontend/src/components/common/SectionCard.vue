<script setup lang="ts">
withDefaults(defineProps<{ title?: string; subtitle?: string; flat?: boolean; padding?: string }>(), { title: '', subtitle: '', flat: false, padding: '20px' })
</script>

<template>
  <section class="section-card" :class="{ 'gm-card': !flat }" :style="{ padding }">
    <header v-if="title || $slots.header || $slots.extra" class="section-card__header">
      <div>
        <slot name="header">
          <h2 class="section-card__title">{{ title }}</h2>
          <p v-if="subtitle" class="section-card__subtitle">{{ subtitle }}</p>
        </slot>
      </div>
      <div v-if="$slots.extra" class="section-card__extra"><slot name="extra" /></div>
    </header>
    <slot />
  </section>
</template>

<style scoped lang="scss">
.section-card {
  &__header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 12px;
    margin-bottom: 16px;
  }
  &__title {
    font-size: 17px;
    font-weight: 700;
    margin: 0;
  }
  &__subtitle {
    margin: 4px 0 0;
    color: var(--gm-text-muted);
    font-size: 13px;
  }
}
</style>
