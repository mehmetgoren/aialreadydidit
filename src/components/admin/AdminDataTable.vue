<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { Paged } from '@/utils/models/common-models'

type T = Record<string, unknown>
import { notifyError } from '@/utils/tools'

/**
 * Server-side data table: search box + filters slot + ElTable (columns via default slot) + pagination.
 * Give it a `fetch(query)` that returns a Paged<T>; it owns q/page/pageSize/sort state and exposes `reload()`.
 */
const props = withDefaults(
  defineProps<{
    fetch: (
      query: {
        q: string
        page: number
        pageSize: number
        sort?: string
        dir?: 'asc' | 'desc'
      } & Record<string, unknown>,
    ) => Promise<Paged<T>>
    /** extra query params (filters) — reloads when they change */
    params?: Record<string, unknown>
    pageSize?: number
    searchable?: boolean
    searchPlaceholder?: string
    rowKey?: string
    selectable?: boolean
    autoLoad?: boolean
    defaultSort?: { prop: string; order: 'ascending' | 'descending' }
  }>(),
  {
    params: () => ({}),
    pageSize: 25,
    searchable: true,
    searchPlaceholder: '',
    rowKey: 'id',
    selectable: false,
    autoLoad: true,
    defaultSort: undefined,
  },
)
const emit = defineEmits<{ selection: [rows: T[]]; loaded: [page: Paged<T>] }>()
const { t } = useI18n()

const q = ref('')
const page = ref(1)
const size = ref(props.pageSize)
const sort = ref<string | undefined>(props.defaultSort?.prop)
const dir = ref<'asc' | 'desc' | undefined>(
  props.defaultSort ? (props.defaultSort.order === 'ascending' ? 'asc' : 'desc') : undefined,
)
const loading = ref(false)
const data = ref<Paged<T>>({
  items: [],
  page: 1,
  pageSize: props.pageSize,
  totalCount: 0,
  totalPages: 0,
})
const rows = computed(() => data.value.items)

async function reload(resetPage = false) {
  if (resetPage) page.value = 1
  loading.value = true
  try {
    data.value = await props.fetch({
      q: q.value.trim(),
      page: page.value,
      pageSize: size.value,
      sort: sort.value,
      dir: dir.value,
      ...props.params,
    })
    emit('loaded', data.value as Paged<T>)
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
}

let timer: ReturnType<typeof setTimeout> | null = null
watch(q, () => {
  if (timer) clearTimeout(timer)
  timer = setTimeout(() => reload(true), 350)
})
watch(
  () => props.params,
  () => reload(true),
  { deep: true },
)
watch([page, size], () => reload())

function onSort({ prop, order }: { prop: string | null; order: string | null }) {
  sort.value = order && prop ? prop : undefined
  dir.value = order === 'ascending' ? 'asc' : order === 'descending' ? 'desc' : undefined
  reload(true)
}

if (props.autoLoad) reload()
defineExpose({ reload, rows, loading })
</script>

<template>
  <div class="adt">
    <div v-if="searchable || $slots.filters || $slots.toolbar" class="adt__toolbar">
      <ElInput
        v-if="searchable"
        v-model="q"
        :placeholder="searchPlaceholder || t('admin_search')"
        clearable
        class="adt__search"
      >
        <template #prefix
          ><ElIcon><Search /></ElIcon
        ></template>
      </ElInput>
      <div class="adt__filters"><slot name="filters" /></div>
      <div class="adt__spacer" />
      <div class="adt__actions">
        <slot name="toolbar" />
        <ElButton text circle :loading="loading" @click="reload()"
          ><ElIcon><Refresh /></ElIcon
        ></ElButton>
      </div>
    </div>
    <ElTable
      v-loading="loading"
      :data="rows"
      :row-key="rowKey"
      :default-sort="defaultSort"
      class="gm-table adt__table"
      stripe
      size="small"
      @sort-change="onSort"
      @selection-change="emit('selection', $event as T[])"
    >
      <ElTableColumn v-if="selectable" type="selection" width="40" />
      <slot />
      <template #empty>
        <slot name="empty"
          ><span class="gm-muted">{{ t('no_data') }}</span></slot
        >
      </template>
    </ElTable>
    <div class="adt__footer">
      <span class="gm-muted adt__count">{{ t('adm_total_records', { n: data.totalCount }) }}</span>
      <ElPagination
        v-model:current-page="page"
        v-model:page-size="size"
        :total="data.totalCount"
        :page-sizes="[10, 25, 50, 100]"
        layout="sizes, prev, pager, next, jumper"
        background
        small
      />
    </div>
  </div>
</template>

<style scoped lang="scss">
.adt {
  &__toolbar {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 8px;
    margin-bottom: 10px;
  }
  &__search {
    width: 260px;
  }
  &__filters {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 8px;
  }
  &__spacer {
    flex: 1;
  }
  &__actions {
    display: flex;
    align-items: center;
    gap: 6px;
  }
  &__footer {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    margin-top: 10px;
    flex-wrap: wrap;
  }
  &__count {
    font-size: 12px;
  }
}
</style>
