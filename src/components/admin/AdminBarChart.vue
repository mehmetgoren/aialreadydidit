<script setup lang="ts">
import { computed } from 'vue'

/** Dependency-free bar/line chart for admin dashboards. `series` = [{ label, values[] }], `labels` = x axis. */
const props = withDefaults(
  defineProps<{
    labels: string[]
    series: { label: string; values: number[]; color?: string }[]
    height?: number
    format?: (v: number) => string
    type?: 'bar' | 'line'
  }>(),
  { height: 220, format: (v: number) => String(Math.round(v)), type: 'bar' },
)
const colors = ['#1f6feb', '#6e56cf', '#2ea043', '#e5484d', '#f2a900']
const max = computed(() => Math.max(1, ...props.series.flatMap((s) => s.values)))
const width = 720
const pad = { l: 8, r: 8, t: 10, b: 24 }
const innerW = width - pad.l - pad.r
const innerH = computed(() => props.height - pad.t - pad.b)
const step = computed(() => innerW / Math.max(1, props.labels.length))
function y(v: number) {
  return pad.t + innerH.value - (v / max.value) * innerH.value
}
function points(vals: number[]) {
  return vals.map((v, i) => `${pad.l + step.value * i + step.value / 2},${y(v)}`).join(' ')
}
const gridLines = computed(() =>
  [0.25, 0.5, 0.75, 1].map((f) => ({ y: y(max.value * f), v: max.value * f })),
)
</script>

<template>
  <div class="chart">
    <div class="chart__legend">
      <span v-for="(s, i) in series" :key="s.label"
        ><i :style="{ background: s.color || colors[i % colors.length] }" />{{ s.label }}</span
      >
    </div>
    <svg :viewBox="`0 0 ${width} ${height}`" class="chart__svg" preserveAspectRatio="none">
      <g v-for="g in gridLines" :key="g.y">
        <line
          :x1="pad.l"
          :x2="width - pad.r"
          :y1="g.y"
          :y2="g.y"
          stroke="currentColor"
          opacity="0.08"
        />
      </g>
      <template v-if="type === 'bar'">
        <g v-for="(s, si) in series" :key="s.label">
          <rect
            v-for="(v, i) in s.values"
            :key="i"
            :x="pad.l + step * i + (step / (series.length + 1)) * (si + 0.5)"
            :y="y(v)"
            :width="step / (series.length + 1)"
            :height="Math.max(0, pad.t + innerH - y(v))"
            :fill="s.color || colors[si % colors.length]"
            rx="3"
          >
            <title>{{ labels[i] }}: {{ format(v) }}</title>
          </rect>
        </g>
      </template>
      <template v-else>
        <polyline
          v-for="(s, si) in series"
          :key="s.label"
          :points="points(s.values)"
          fill="none"
          :stroke="s.color || colors[si % colors.length]"
          stroke-width="2.5"
        />
        <g v-for="(s, si) in series" :key="`p${s.label}`">
          <circle
            v-for="(v, i) in s.values"
            :key="i"
            :cx="pad.l + step * i + step / 2"
            :cy="y(v)"
            r="3"
            :fill="s.color || colors[si % colors.length]"
          >
            <title>{{ labels[i] }}: {{ format(v) }}</title>
          </circle>
        </g>
      </template>
      <text
        v-for="(l, i) in labels"
        :key="l + i"
        :x="pad.l + step * i + step / 2"
        :y="height - 6"
        text-anchor="middle"
        font-size="10"
        fill="currentColor"
        opacity="0.6"
      >
        {{ labels.length > 16 && i % Math.ceil(labels.length / 16) !== 0 ? '' : l }}
      </text>
    </svg>
  </div>
</template>

<style scoped lang="scss">
.chart {
  width: 100%;
  color: var(--gm-text);
  &__svg {
    width: 100%;
    height: auto;
    display: block;
  }
  &__legend {
    display: flex;
    gap: 14px;
    font-size: 12px;
    color: var(--gm-text-muted);
    margin-bottom: 6px;
    i {
      display: inline-block;
      width: 10px;
      height: 10px;
      border-radius: 2px;
      margin-right: 5px;
    }
  }
}
</style>
