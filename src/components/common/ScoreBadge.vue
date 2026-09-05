<script setup lang="ts">
import { computed } from 'vue'

/** Rating out of 100 as a coloured pill; grey when nobody rated yet. */
const props = withDefaults(defineProps<{ score: number; count?: number; large?: boolean }>(), { count: 0, large: false })
const tone = computed(() => (props.count === 0 ? 'none' : props.score >= 80 ? 'good' : props.score >= 55 ? 'mid' : 'bad'))
</script>

<template>
  <span class="score" :class="[`is-${tone}`, { 'is-large': large }]">
    <ElIcon><StarFilled /></ElIcon>
    <template v-if="count > 0">{{ Math.round(score) }}<small>/100</small><span v-if="!large" class="score__count">({{ count }})</span></template>
    <template v-else>—</template>
  </span>
</template>

<style scoped lang="scss">
.score {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  padding: 2px 8px;
  border-radius: 999px;
  font-weight: 700;
  font-size: 12px;
  background: var(--gm-page-bg);
  color: var(--gm-text-muted);
  small {
    font-weight: 500;
    opacity: 0.8;
  }
  &__count {
    font-weight: 500;
    margin-left: 3px;
    opacity: 0.8;
  }
  &.is-good {
    background: var(--gm-green-light);
    color: var(--gm-green-dark);
  }
  &.is-mid {
    background: #fff4d6;
    color: #a86b00;
  }
  &.is-bad {
    background: #fde3e3;
    color: #b42318;
  }
  &.is-large {
    font-size: 18px;
    padding: 6px 14px;
  }
}
</style>
