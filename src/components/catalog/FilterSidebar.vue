<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useCategoryStore, useCategoryName } from '@/stores/category-store'
import { useSiteStore } from '@/stores/site-store'
import type { AppQuery, CategoryNode, FacetItem } from '@/utils/models/catalog-models'
import { platformIcon } from '@/utils/tools'

/**
 * sahibinden.com-style left sidebar: category tree first, then platform / license / model / rating / tags refinements.
 * Emits a patched query; the page owns the state and the URL.
 */
const props = defineProps<{
  query: AppQuery
  category: CategoryNode | null
  facets: { platforms: FacetItem[]; licenses: FacetItem[]; models: FacetItem[]; categories: FacetItem[] }
}>()
const emit = defineEmits<{ update: [patch: Partial<AppQuery>] }>()
const { t } = useI18n()
const categories = useCategoryStore()
const site = useSiteStore()
const name = useCategoryName()

const path = computed(() => (props.category ? categories.pathTo(props.category.slug) : []))
const children = computed(() => (props.category ? props.category.children : categories.roots))
const platforms = computed(() =>
  site.platforms.map((p) => ({ ...p, count: props.facets.platforms.find((f) => f.value === p.code)?.count ?? 0 })).filter((p) => p.count > 0 || props.query.platform === p.code),
)
const tags = computed(() => (props.query.tags ?? '').split(',').map((s) => s.trim()).filter(Boolean))

function set<K extends keyof AppQuery>(key: K, value: AppQuery[K]) {
  emit('update', { [key]: props.query[key] === value ? undefined : value, page: 1 } as Partial<AppQuery>)
}
function removeTag(tag: string) {
  emit('update', { tags: tags.value.filter((x) => x !== tag).join(',') || undefined, page: 1 })
}
</script>

<template>
  <aside class="filters">
    <div class="filters__block">
      <div class="filters__title">{{ t('categories') }}</div>
      <div class="filters__path">
        <a class="filters__path-item" @click="emit('update', { category: undefined, page: 1 })">{{ t('all_categories') }}</a>
        <template v-for="node in path" :key="node.id">
          <span class="gm-muted">›</span>
          <a class="filters__path-item" :class="{ 'is-current': node.id === category?.id }" @click="emit('update', { category: node.slug, page: 1 })">{{ name(node) }}</a>
        </template>
      </div>
      <ul class="filters__list">
        <li v-for="node in children" :key="node.id">
          <a @click="emit('update', { category: node.slug, page: 1 })">{{ name(node) }}</a>
          <span class="filters__count">{{ node.appCount }}</span>
        </li>
      </ul>
    </div>

    <div v-if="platforms.length" class="filters__block">
      <div class="filters__title">{{ t('platform') }}</div>
      <ul class="filters__list">
        <li v-for="p in platforms" :key="p.code" :class="{ 'is-active': query.platform === p.code }">
          <a @click="set('platform', p.code)">{{ platformIcon(p.code) }} {{ p.name }}</a>
          <span class="filters__count">{{ p.count }}</span>
        </li>
      </ul>
    </div>

    <div v-if="facets.licenses.length" class="filters__block">
      <div class="filters__title">{{ t('license') }}</div>
      <ul class="filters__list">
        <li v-for="l in facets.licenses" :key="l.value" :class="{ 'is-active': query.license === l.value }">
          <a @click="set('license', l.value)">{{ l.value }}</a>
          <span class="filters__count">{{ l.count }}</span>
        </li>
      </ul>
    </div>

    <div v-if="facets.models.length" class="filters__block">
      <div class="filters__title">{{ t('generated_by') }}</div>
      <ul class="filters__list">
        <li v-for="m in facets.models" :key="m.value" :class="{ 'is-active': query.model === m.value }">
          <a @click="set('model', m.value)">{{ m.label }}</a>
          <span class="filters__count">{{ m.count }}</span>
        </li>
      </ul>
    </div>

    <div class="filters__block">
      <div class="filters__title">{{ t('rating') }}</div>
      <ul class="filters__list">
        <li v-for="r in [90, 80, 60]" :key="r" :class="{ 'is-active': query.minRating === r }">
          <a @click="set('minRating', r)">★ {{ r }}+</a>
        </li>
      </ul>
    </div>

    <div v-if="tags.length" class="filters__block">
      <div class="filters__title">{{ t('tags') }}</div>
      <div class="filters__tags">
        <ElTag v-for="tag in tags" :key="tag" closable size="small" @close="removeTag(tag)">{{ tag }}</ElTag>
      </div>
    </div>
  </aside>
</template>

<style scoped lang="scss">
.filters {
  &__block {
    background: var(--gm-card-bg);
    border: 1px solid var(--gm-border);
    border-radius: 10px;
    padding: 12px 14px;
    margin-bottom: 12px;
  }
  &__title {
    font-weight: 700;
    font-size: 13px;
    margin-bottom: 8px;
    text-transform: uppercase;
    letter-spacing: 0.3px;
    color: var(--gm-text-muted);
  }
  &__path {
    display: flex;
    flex-wrap: wrap;
    gap: 4px;
    font-size: 13px;
    margin-bottom: 8px;
    &-item {
      cursor: pointer;
      color: var(--gm-primary);
      &.is-current {
        color: var(--gm-text);
        font-weight: 700;
      }
    }
  }
  &__list {
    list-style: none;
    margin: 0;
    padding: 0;
    li {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 5px 6px;
      border-radius: 6px;
      font-size: 13px;
      a {
        cursor: pointer;
        color: var(--gm-text);
      }
      &:hover {
        background: var(--gm-page-bg);
      }
      &.is-active {
        background: var(--gm-primary-light);
        a {
          color: var(--gm-primary);
          font-weight: 700;
        }
      }
    }
  }
  &__count {
    font-size: 11px;
    color: var(--gm-text-muted);
  }
  &__tags {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
  }
}
</style>
