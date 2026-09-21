<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useCategoryStore, useCategoryName } from '@/stores/category-store'
import { useCommonStore } from '@/stores/common-store'
import { useSiteStore } from '@/stores/site-store'
import { useAppBrowser } from './use-app-browser'
import { useMediaQuery } from '@/composables/use-media-query'
import FilterSidebar from '@/components/catalog/FilterSidebar.vue'
import AppGrid from '@/components/catalog/AppGrid.vue'
import PagePagination from '@/components/common/PagePagination.vue'

/** Search / browse page — category-first sidebar (sahibinden style) + hybrid keyword/semantic results. */
const props = withDefaults(defineProps<{ fixedCategory?: string }>(), { fixedCategory: '' })
const { t } = useI18n()
const categories = useCategoryStore()
const common = useCommonStore()
const site = useSiteStore()
const name = useCategoryName()
const { query, result, loading, update } = useAppBrowser(() => (props.fixedCategory ? { category: props.fixedCategory } : {}))
const localQ = ref(query.value.q ?? '')
watch(() => query.value.q, (v) => (localQ.value = v ?? ''))

const category = computed(() => (query.value.category ? categories.bySlug.get(query.value.category) ?? null : null))
const sortOptions = computed(() => [
  { value: 'relevance', label: t('sort_relevance') },
  { value: 'downloads', label: t('sort_downloads') },
  { value: 'rating', label: t('sort_rating') },
  { value: 'newest', label: t('sort_newest') },
  { value: 'updated', label: t('sort_updated') },
  { value: 'name', label: t('sort_name') },
])

onMounted(async () => {
  await categories.fetchTree().catch(() => {})
})
watch(
  [category, () => query.value.q],
  () => {
    const title = category.value ? name(category.value) : query.value.q ? `${t('search')}: ${query.value.q}` : t('search')
    common.setPageTitle(title)
    common.setBreadcrumb(category.value ? categories.pathTo(category.value.slug).map((c) => ({ label: name(c), to: `/category/${c.slug}` })) : [{ label: t('search') }])
  },
  { immediate: true },
)

// phones: the sidebar would push the results two screens down, so it lives in a drawer behind a "Filters" button
const narrow = useMediaQuery('(max-width: 860px)')
const filtersOpen = ref(false)
const activeFilters = computed(() => {
  const v = query.value
  const picked = [v.platform, v.license, v.model, v.minRating, v.tags, props.fixedCategory ? undefined : v.category]
  return picked.filter((x) => x !== undefined && x !== null && x !== '').length
})
watch(narrow, (v) => {
  if (!v) filtersOpen.value = false
})

function submit() {
  update({ q: localQ.value.trim() || undefined, page: 1 })
}
</script>

<template>
  <div class="gm-container search">
    <div class="search__bar">
      <form class="search__form" @submit.prevent="submit">
        <ElInput v-model="localQ" size="large" :placeholder="t('search_placeholder')" clearable @clear="submit">
          <template #prefix><ElIcon><Search /></ElIcon></template>
          <template #append><ElButton type="primary" native-type="submit">{{ t('search') }}</ElButton></template>
        </ElInput>
      </form>
      <ElRadioGroup v-if="query.q && site.semantic" class="search__mode" :model-value="query.mode ?? 'hybrid'" size="small" @update:model-value="update({ mode: $event as never, page: 1 })">
        <ElRadioButton value="hybrid">{{ t('mode_hybrid') }}</ElRadioButton>
        <ElRadioButton value="semantic">{{ t('mode_semantic') }}</ElRadioButton>
        <ElRadioButton value="keyword">{{ t('mode_keyword') }}</ElRadioButton>
      </ElRadioGroup>
      <ElButton v-if="narrow" class="search__filters-btn" @click="filtersOpen = true">
        <ElIcon><Filter /></ElIcon>{{ t('filters') }}<span v-if="activeFilters" class="search__filters-count">{{ activeFilters }}</span>
      </ElButton>
      <ElSelect :model-value="query.sort ?? (query.q ? 'relevance' : 'downloads')" size="default" class="search__sort" @update:model-value="update({ sort: $event, page: 1 })">
        <ElOption v-for="o in sortOptions" :key="o.value" :value="o.value" :label="o.label" />
      </ElSelect>
    </div>

    <div class="search__layout">
      <FilterSidebar v-if="!narrow" :query="query" :category="category" :facets="result" @update="update" />
      <div class="search__results">
        <div class="search__meta gm-muted">
          <span v-if="!loading">{{ t('results_count', { n: result.page.totalCount }) }}</span>
          <span v-if="query.q && result.modeUsed !== 'browse'"> · {{ t('mode_used', { mode: t(`mode_${result.modeUsed}`) }) }}</span>
        </div>
        <AppGrid :apps="result.page.items" :loading="loading" :show-similarity="Boolean(query.q)" :empty-title="t('no_results_title')" :empty-text="t('no_results_text')">
          <template #empty>
            <RouterLink :to="{ path: '/wanted', query: { title: query.q } }"><ElButton type="primary">{{ t('post_wanted') }}</ElButton></RouterLink>
          </template>
        </AppGrid>
        <PagePagination :page="result.page.page" :page-size="result.page.pageSize" :total="result.page.totalCount" @update:page="update({ page: $event })" />
      </div>
    </div>

    <ElDrawer v-if="narrow" v-model="filtersOpen" direction="btt" size="82%" :title="t('filters')" class="search__drawer" append-to-body>
      <FilterSidebar :query="query" :category="category" :facets="result" @update="update" />
      <template #footer>
        <ElButton type="primary" size="large" class="search__drawer-done" :loading="loading" @click="filtersOpen = false">
          {{ t('show_results', { n: result.page.totalCount }) }}
        </ElButton>
      </template>
    </ElDrawer>
  </div>
</template>

<style scoped lang="scss">
.search {
  padding-top: 12px;
  &__bar {
    display: flex;
    gap: 12px;
    align-items: center;
    margin-bottom: 16px;
    flex-wrap: wrap;
  }
  &__form {
    flex: 1;
    min-width: 260px;
    :deep(.el-input-group__append) {
      background: var(--gm-primary);
      border-color: var(--gm-primary);
      .el-button { color: #fff; }
    }
  }
  &__sort {
    width: 180px;
  }
  &__layout {
    display: grid;
    grid-template-columns: 260px 1fr;
    gap: 18px;
    align-items: start;
  }
  &__meta {
    font-size: 13px;
    margin-bottom: 10px;
  }
  &__filters-count {
    display: inline-grid;
    place-items: center;
    min-width: 18px;
    height: 18px;
    margin-inline-start: 6px;
    padding: 0 5px;
    border-radius: 9px;
    background: var(--gm-primary);
    color: #fff;
    font-size: 11px;
    font-weight: 700;
  }
  @media (max-width: 860px) {
    &__layout {
      grid-template-columns: 1fr;
    }
    &__bar {
      gap: 10px;
    }
    &__form {
      flex: 1 1 100%;
      min-width: 0;
    }
    // the mode switch gets its own full-width row so "Filters" and the sort select share the next one evenly
    &__mode {
      display: flex;
      flex: 1 1 100%;
      :deep(.el-radio-button) {
        flex: 1;
      }
      :deep(.el-radio-button__inner) {
        width: 100%;
      }
    }
    &__filters-btn {
      flex: 1 1 0;
      .el-icon {
        margin-inline-end: 6px;
      }
    }
    &__sort {
      flex: 1 1 0;
      width: auto;
      min-width: 0;
    }
  }
}
</style>

<style lang="scss">
// the drawer is teleported to <body>, so it cannot be reached from the scoped block
.search__drawer {
  border-radius: 16px 16px 0 0;
  .el-drawer__header {
    margin-bottom: 0;
    padding: 16px 16px 8px;
    font-weight: 700;
    color: var(--gm-text);
  }
  .el-drawer__body {
    padding: 8px 16px 16px;
  }
  .el-drawer__footer {
    padding: 10px 16px calc(12px + env(safe-area-inset-bottom));
    border-top: 1px solid var(--gm-border);
  }
  .search__drawer-done {
    width: 100%;
  }
}
</style>
