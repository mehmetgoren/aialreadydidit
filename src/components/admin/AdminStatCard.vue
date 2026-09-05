<script setup lang="ts">
withDefaults(
  defineProps<{
    label: string
    value: string | number
    icon?: string
    hint?: string
    delta?: number | null
    tone?: 'default' | 'success' | 'warning' | 'danger' | 'purple'
    to?: string
  }>(),
  { icon: 'DataLine', hint: '', delta: null, tone: 'default', to: '' },
)
</script>

<template>
  <component
    :is="to ? 'RouterLink' : 'div'"
    :to="to || undefined"
    class="stat"
    :class="`is-${tone}`"
  >
    <div class="stat__icon">
      <ElIcon :size="20"><component :is="icon" /></ElIcon>
    </div>
    <div class="stat__body">
      <div class="stat__label">{{ label }}</div>
      <div class="stat__value">{{ value }}</div>
      <div v-if="hint || delta !== null" class="stat__hint">
        <span v-if="delta !== null" class="stat__delta" :class="delta >= 0 ? 'is-up' : 'is-down'"
          >{{ delta >= 0 ? '▲' : '▼' }} %{{ Math.abs(delta).toFixed(1).replace('.', ',') }}</span
        >
        <span v-if="hint">{{ hint }}</span>
      </div>
    </div>
  </component>
</template>

<style scoped lang="scss">
.stat {
  display: flex;
  gap: 12px;
  align-items: flex-start;
  padding: 14px 16px;
  border: 1px solid var(--gm-border);
  border-radius: 10px;
  background: var(--gm-card-bg);
  color: var(--gm-text);
  text-decoration: none;
  min-width: 0;
  &__icon {
    width: 40px;
    height: 40px;
    border-radius: 10px;
    display: grid;
    place-items: center;
    background: var(--gm-purple-light);
    color: var(--gm-purple);
    flex-shrink: 0;
  }
  &.is-success .stat__icon {
    background: #e3f6ea;
    color: var(--gm-green);
  }
  &.is-warning .stat__icon {
    background: #fff3d6;
    color: #d98a00;
  }
  &.is-danger .stat__icon {
    background: #fde3e3;
    color: var(--el-color-danger);
  }
  &__label {
    font-size: 12px;
    color: var(--gm-text-muted);
  }
  &__value {
    font-size: 22px;
    font-weight: 700;
    line-height: 1.2;
    margin-top: 2px;
    white-space: nowrap;
  }
  &__hint {
    font-size: 11px;
    color: var(--gm-text-muted);
    margin-top: 4px;
    display: flex;
    gap: 6px;
  }
  &__delta.is-up {
    color: var(--gm-green);
    font-weight: 600;
  }
  &__delta.is-down {
    color: var(--el-color-danger);
    font-weight: 600;
  }
}
a.stat:hover {
  box-shadow: var(--gm-shadow);
}
</style>
