<script setup lang="ts">
withDefaults(defineProps<{ title?: string; text?: string; icon?: string }>(), { title: '', text: '', icon: 'Search' })
</script>

<template>
  <div class="empty-state">
    <div class="empty-state__art">
      <ElIcon :size="42"><component :is="icon" /></ElIcon>
    </div>
    <h3 v-if="title" class="empty-state__title">{{ title }}</h3>
    <p v-if="text" class="empty-state__text">{{ text }}</p>
    <div v-if="$slots.default" class="empty-state__actions"><slot /></div>
  </div>
</template>

<style scoped lang="scss">
.empty-state {
  text-align: center;
  padding: 44px 24px;
  &__art {
    display: grid;
    place-items: center;
    width: 84px;
    height: 84px;
    margin: 0 auto 16px;
    border-radius: 50%;
    background: var(--gm-primary-light);
    color: var(--gm-primary);
  }
  &__title {
    font-size: 17px;
    font-weight: 700;
    margin: 0 0 8px;
  }
  &__text {
    color: var(--gm-text-muted);
    max-width: 480px;
    margin: 0 auto;
    line-height: 1.6;
  }
  &__actions {
    margin-top: 16px;
  }
}
</style>
