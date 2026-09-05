<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useCategoryStore, useCategoryName } from '@/stores/category-store'
import type { CategoryNode } from '@/utils/models/catalog-models'

const { t } = useI18n()
const categories = useCategoryStore()
const name = useCategoryName()
const open = ref<CategoryNode | null>(null)
let timer: ReturnType<typeof setTimeout> | null = null

onMounted(() => {
  categories.fetchTree().catch(() => {})
})

function enter(node: CategoryNode) {
  if (timer) clearTimeout(timer)
  open.value = node
}
function leave() {
  timer = setTimeout(() => (open.value = null), 120)
}
</script>

<template>
  <nav class="catnav" @mouseleave="leave">
    <div class="gm-container catnav__inner">
      <RouterLink to="/search" class="catnav__all">
        <ElIcon><Menu /></ElIcon>{{ t('all_categories') }}
      </RouterLink>
      <RouterLink
        v-for="root in categories.roots"
        :key="root.id"
        :to="`/category/${root.slug}`"
        class="catnav__item"
        :class="{ 'is-open': open?.id === root.id }"
        @mouseenter="enter(root)"
      >
        {{ name(root) }}
      </RouterLink>
    </div>

    <Transition name="fade">
      <div v-if="open && open.children.length" class="catnav__mega" @mouseenter="enter(open)" @mouseleave="leave">
        <div class="gm-container catnav__mega-inner">
          <div v-for="sub in open.children" :key="sub.id" class="catnav__col">
            <RouterLink :to="`/category/${sub.slug}`" class="catnav__sub" @click="open = null">
              {{ name(sub) }} <span class="gm-muted">· {{ sub.appCount }}</span>
            </RouterLink>
            <RouterLink v-for="leaf in sub.children.slice(0, 8)" :key="leaf.id" :to="`/category/${leaf.slug}`" class="catnav__leaf" @click="open = null">
              {{ name(leaf) }}
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
    display: flex;
    align-items: center;
    gap: 4px;
    height: var(--gm-nav-height);
    overflow-x: auto;
    scrollbar-width: none;
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
  }
  &__item {
    color: var(--gm-text);
    font-size: 13px;
    padding: 6px 9px;
    border-radius: 6px;
    white-space: nowrap;
    &.is-open,
    &:hover {
      background: var(--gm-primary-light);
      color: var(--gm-primary);
    }
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
