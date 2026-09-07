<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { SavingsDto } from '@/utils/models/catalog-models'
import { formatCompact, formatMoney, formatNumber } from '@/utils/format'

/** The mission counter: animates from 0 to the estimated tokens / cost / energy saved. */
const props = withDefaults(defineProps<{ savings: SavingsDto | null; compact?: boolean }>(), { compact: false })
const { t } = useI18n()
const shown = ref(0)
let raf = 0

function animate(target: number) {
  cancelAnimationFrame(raf)
  const start = performance.now()
  const from = shown.value
  const duration = 1400
  const step = (now: number) => {
    const p = Math.min(1, (now - start) / duration)
    const eased = 1 - Math.pow(1 - p, 3)
    shown.value = Math.round(from + (target - from) * eased)
    if (p < 1) raf = requestAnimationFrame(step)
  }
  raf = requestAnimationFrame(step)
}

watch(
  () => props.savings?.tokensSaved,
  (v) => {
    if (v !== undefined) animate(v)
  },
  { immediate: true },
)
onMounted(() => {
  if (props.savings) animate(props.savings.tokensSaved)
})
onUnmounted(() => cancelAnimationFrame(raf))

const cost = computed(() => props.savings?.costSavedUsd ?? 0)
</script>

<template>
  <div class="savings" :class="{ 'is-compact': compact }">
    <div class="savings__main">
      <div class="savings__label">{{ t('savings_tokens_saved') }}</div>
      <div class="savings__value">{{ formatNumber(shown) }}</div>
      <div class="savings__hint">{{ t('savings_hint') }}</div>
    </div>
    <div class="savings__side">
      <div class="savings__item">
        <span class="savings__k">{{ t('savings_money') }}</span>
        <span class="savings__v">{{ formatMoney(cost) }}</span>
      </div>
      <div class="savings__item">
        <span class="savings__k">{{ t('savings_energy') }}</span>
        <span class="savings__v">{{ formatNumber(savings?.kwhSaved ?? 0, 2) }} kWh</span>
      </div>
      <div class="savings__item">
        <span class="savings__k">CO₂</span>
        <span class="savings__v">{{ formatNumber(savings?.co2SavedKg ?? 0, 2) }} kg</span>
      </div>
      <div class="savings__item">
        <span class="savings__k">{{ t('savings_downloads') }}</span>
        <span class="savings__v">{{ formatCompact(savings?.totalDownloads ?? 0) }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.savings {
  display: grid;
  grid-template-columns: 1.3fr 1fr;
  gap: 20px;
  padding: 22px 26px;
  border-radius: 14px;
  background: linear-gradient(135deg, var(--gm-navy), #1f3d7a 60%, #1f6feb);
  color: #fff;
  &__label {
    font-size: 13px;
    opacity: 0.85;
    text-transform: uppercase;
    letter-spacing: 0.5px;
  }
  &__value {
    font-size: 44px;
    font-weight: 800;
    letter-spacing: -1px;
    line-height: 1.1;
    font-variant-numeric: tabular-nums;
  }
  &__hint {
    font-size: 12px;
    opacity: 0.8;
    margin-top: 6px;
    max-width: 440px;
  }
  &__side {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 10px 16px;
    align-content: center;
  }
  &__item {
    display: flex;
    flex-direction: column;
  }
  &__k {
    font-size: 12px;
    opacity: 0.8;
  }
  &__v {
    font-size: 18px;
    font-weight: 700;
  }
  &.is-compact {
    grid-template-columns: 1fr;
    padding: 14px 18px;
    .savings__value {
      font-size: 30px;
    }
    .savings__side {
      grid-template-columns: repeat(4, 1fr);
    }
  }
  @media (max-width: 760px) {
    grid-template-columns: 1fr;
    &__value {
      font-size: 34px;
    }
  }
}
</style>
