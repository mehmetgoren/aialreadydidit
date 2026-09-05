<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useCategoryStore, useCategoryName } from '@/stores/category-store'
import { useCommonStore } from '@/stores/common-store'
import { useSiteStore } from '@/stores/site-store'
import { useAppBrowser } from './use-app-browser'
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
      <ElRadioGroup v-if="query.q && site.semantic" :model-value="query.mode ?? 'hybrid'" size="small" @update:model-value="update({ mode: $event as never, page: 1 })">
        <ElRadioButton value="hybrid">{{ t('mode_hybrid') }}</ElRadioButton>
        <ElRadioButton value="semantic">{{ t('mode_semantic') }}</ElRadioButton>
        <ElRadioButton value="keyword">{{ t('mode_keyword') }}</ElRadioButton>
      </ElRadioGroup>
      <ElSelect :model-value="query.sort ?? (query.q ? 'relevance' : 'downloads')" size="default" class="search__sort" @update:model-value="update({ sort: $event, page: 1 })">
        <ElOption v-for="o in sortOptions" :key="o.value" :value="o.value" :label="o.label" />
      </ElSelect>
    </div>

    <div class="search__layout">
      <FilterSidebar :query="query" :category="category" :facets="result" @update="update" />
      <div class="search__results">
        <div class="search__meta gm-muted">
          <span>{{ t('results_count', { n: result.page.totalCount }) }}</span>
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
  @media (max-width: 860px) {
    &__layout {
      grid-template-columns: 1fr;
    }
  }
}
</style>
