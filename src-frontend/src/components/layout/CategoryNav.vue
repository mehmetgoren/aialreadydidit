<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useResizeObserver } from '@vueuse/core'
import { useCategoryStore, useCategoryName } from '@/stores/category-store'
import type { CategoryNode } from '@/utils/models/catalog-models'

/**
 * Category-first navigation bar (sahibinden / Play-store style).
 * Root categories that do not fit on one line collapse into a "More" menu instead of overflowing off-screen;
 * hovering a root opens a mega panel with its sub-categories.
 */
const { t, locale } = useI18n()
const categories = useCategoryStore()
const name = useCategoryName()

const open = ref<CategoryNode | null>(null)
const moreOpen = ref(false)
let timer: ReturnType<typeof setTimeout> | null = null

const inner = ref<HTMLElement | null>(null)
const allEl = ref<unknown>(null) // RouterLink instance; resolved to its root element via toEl()
const itemEls = new Map<number, HTMLElement>()
const moreEl = ref<HTMLElement | null>(null)
const visibleCount = ref(Number.MAX_SAFE_INTEGER)
const GAP = 4

const hiddenRoots = computed(() => categories.roots.slice(visibleCount.value))

/** Template refs on components (RouterLink) give the instance; take its root element. */
function toEl(el: unknown): HTMLElement | null {
  return (el as { $el?: HTMLElement } | null)?.$el ?? (el as HTMLElement | null)
}

function setItem(id: number, el: unknown) {
  const node = toEl(el)
  if (node) itemEls.set(id, node)
  else itemEls.delete(id)
}

/** Fit as many roots as the row allows; reserve the measured width of the "More" item when something must be hidden. */
function measure() {
  const container = inner.value
  const roots = categories.roots
  if (!container || roots.length === 0) return
  const widths = roots.map((r) => (itemEls.get(r.id)?.offsetWidth ?? 0) + GAP)
  const style = getComputedStyle(container)
  const padding = parseFloat(style.paddingLeft) + parseFloat(style.paddingRight)
  const allNode = toEl(allEl.value)
  const all = allNode ? allNode.offsetWidth + parseFloat(getComputedStyle(allNode).marginRight) : 0
  const budget = container.clientWidth - padding - all - GAP
  const total = widths.reduce((a, b) => a + b, 0)
  if (total <= budget) {
    visibleCount.value = roots.length
    return
  }
  const moreWidth = (moreEl.value?.offsetWidth ?? 80) + GAP
  let used = 0
  let count = 0
  for (const w of widths) {
    if (used + w > budget - moreWidth) break
    used += w
    count++
  }
  visibleCount.value = count
}

async function remeasure() {
  visibleCount.value = Number.MAX_SAFE_INTEGER // render everything so each item can be measured
  await nextTick()
  measure()
}

onMounted(() => {
  categories.fetchTree().catch(() => {})
})
watch([() => categories.roots, locale], remeasure, { flush: 'post' })
useResizeObserver(inner, () => remeasure())

function enter(node: CategoryNode) {
  if (timer) clearTimeout(timer)
  moreOpen.value = false
  open.value = node
}
function enterMore() {
  if (timer) clearTimeout(timer)
  open.value = null
  moreOpen.value = true
}
function keep() {
  if (timer) clearTimeout(timer)
}
function leave() {
  timer = setTimeout(() => {
    open.value = null
    moreOpen.value = false
  }, 120)
}
function close() {
  open.value = null
  moreOpen.value = false
}
</script>

<template>
  <nav class="catnav" @mouseleave="leave">
    <div ref="inner" class="gm-container catnav__inner">
      <RouterLink ref="allEl" to="/search" class="catnav__all">
        <ElIcon><Menu /></ElIcon>{{ t('all_categories') }}
      </RouterLink>
      <RouterLink
        v-for="(root, index) in categories.roots"
        :key="root.id"
        :ref="(el) => setItem(root.id, el)"
        :to="`/category/${root.slug}`"
        class="catnav__item"
        :class="{ 'is-open': open?.id === root.id, 'is-hidden': index >= visibleCount }"
        :tabindex="index >= visibleCount ? -1 : undefined"
        :aria-hidden="index >= visibleCount ? 'true' : undefined"
        @mouseenter="enter(root)"
        @click="close"
      >
        {{ name(root) }}
      </RouterLink>
      <button
        ref="moreEl"
        type="button"
        class="catnav__item catnav__more"
        :class="{ 'is-open': moreOpen, 'is-hidden': hiddenRoots.length === 0 }"
        :tabindex="hiddenRoots.length === 0 ? -1 : undefined"
        :aria-hidden="hiddenRoots.length === 0 ? 'true' : undefined"
        @mouseenter="enterMore"
        @click="moreOpen = !moreOpen"
      >
        {{ t('more_categories') }} <ElIcon><ArrowDown /></ElIcon>
      </button>
    </div>

    <Transition name="fade">
      <div v-if="open && open.children.length" class="catnav__mega" @mouseenter="keep" @mouseleave="leave">
        <div class="gm-container catnav__mega-inner">
          <div v-for="sub in open.children" :key="sub.id" class="catnav__col">
            <RouterLink :to="`/category/${sub.slug}`" class="catnav__sub" @click="close">
              {{ name(sub) }} <span class="gm-muted">· {{ sub.appCount }}</span>
            </RouterLink>
            <RouterLink v-for="leaf in sub.children.slice(0, 8)" :key="leaf.id" :to="`/category/${leaf.slug}`" class="catnav__leaf" @click="close">
              {{ name(leaf) }}
            </RouterLink>
          </div>
        </div>
      </div>
    </Transition>

    <Transition name="fade">
      <div v-if="moreOpen && hiddenRoots.length" class="catnav__mega" @mouseenter="keep" @mouseleave="leave">
        <div class="gm-container catnav__mega-inner">
          <div v-for="root in hiddenRoots" :key="root.id" class="catnav__col">
            <RouterLink :to="`/category/${root.slug}`" class="catnav__sub" @click="close">
              {{ name(root) }} <span class="gm-muted">· {{ root.appCount }}</span>
            </RouterLink>
            <RouterLink v-for="sub in root.children.slice(0, 6)" :key="sub.id" :to="`/category/${sub.slug}`" class="catnav__leaf" @click="close">
              {{ name(sub) }}
            </RouterLink>
          </div>
        </div>
      </div>
    </Transition>
  </nav>
</template>

<style scoped lang="scss">
.catnav {
  position: relative;
  background: var(--gm-card-bg);
  border-bottom: 1px solid var(--gm-border);
  &__inner {
    position: relative; // contains the absolutely positioned hidden items so they are clipped by the row
    display: flex;
    align-items: center;
    justify-content: space-between; // leftover width spreads between items instead of pooling before "More"
    gap: 4px;
    height: var(--gm-nav-height);
    overflow: hidden;
  }
  &__all {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    font-weight: 700;
    color: var(--gm-text);
    padding: 0 10px 0 0;
    margin-right: 6px;
    border-right: 1px solid var(--gm-border);
    white-space: nowrap;
    flex-shrink: 0;
  }
  &__item {
    color: var(--gm-text);
    font-size: 13px;
    padding: 6px 8px;
    border-radius: 6px;
    white-space: nowrap;
    flex-shrink: 0;
    &.is-open,
    &:hover {
      background: var(--gm-primary-light);
      color: var(--gm-primary);
    }
    // Kept in the DOM (measurable) but out of the visible row.
    &.is-hidden {
      position: absolute;
      visibility: hidden;
      pointer-events: none;
    }
  }
  &__more {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    border: 0;
    background: transparent;
    font: inherit;
    font-size: 13px;
    cursor: pointer;
  }
  &__mega {
    position: absolute;
    left: 0;
    right: 0;
    top: 100%;
    background: var(--gm-card-bg);
    border-bottom: 1px solid var(--gm-border);
    box-shadow: var(--gm-shadow-hover);
    z-index: 30;
    &-inner {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(190px, 1fr));
      gap: 18px 24px;
      padding: 18px 16px 22px;
    }
  }
  &__col {
    display: flex;
    flex-direction: column;
    gap: 4px;
  }
  &__sub {
    font-weight: 700;
    color: var(--gm-text);
    margin-bottom: 4px;
  }
  &__leaf {
    color: var(--gm-text-muted);
    font-size: 13px;
    &:hover {
      color: var(--gm-primary);
    }
  }
}
</style>
