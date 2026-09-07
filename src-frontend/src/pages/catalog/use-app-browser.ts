import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import type { AppQuery, AppSearchResult } from '@/utils/models/catalog-models'
import { CatalogService } from '@/utils/services/catalog-service'
import { notifyError } from '@/utils/tools'

const EMPTY: AppSearchResult = { page: { items: [], page: 1, pageSize: 24, totalCount: 0, totalPages: 0 }, platforms: [], licenses: [], models: [], categories: [], modeUsed: 'browse', semanticAvailable: false }

/** Shared state for the category and search pages: query ↔ URL sync + fetching. */
export function useAppBrowser(fixed: () => Partial<AppQuery>) {
  const route = useRoute()
  const router = useRouter()
  const service = new CatalogService()
  const result = ref<AppSearchResult>(EMPTY)
  const loading = ref(false)

  const query = computed<AppQuery>(() => {
    const q = route.query
    const num = (v: unknown) => (typeof v === 'string' && v !== '' ? Number(v) : undefined)
    return {
      q: typeof q.q === 'string' ? q.q : undefined,
      mode: (typeof q.mode === 'string' ? q.mode : undefined) as AppQuery['mode'],
      category: typeof q.category === 'string' ? q.category : undefined,
      platform: typeof q.platform === 'string' ? q.platform : undefined,
      license: typeof q.license === 'string' ? q.license : undefined,
      model: typeof q.model === 'string' ? q.model : undefined,
      minRating: num(q.minRating),
      tags: typeof q.tags === 'string' ? q.tags : undefined,
      uploader: typeof q.uploader === 'string' ? q.uploader : undefined,
      sort: typeof q.sort === 'string' ? q.sort : undefined,
      page: num(q.page) ?? 1,
      pageSize: 24,
      ...fixed(),
    }
  })

  function update(patch: Partial<AppQuery>) {
    const next: Record<string, string> = {}
    const merged = { ...query.value, ...patch }
    for (const [k, v] of Object.entries(merged)) {
      if (k === 'pageSize' || v === undefined || v === null || v === '' || v === false) continue
      if (k === 'page' && v === 1) continue
      if (k in fixed()) continue
      next[k] = String(v)
    }
    router.push({ path: route.path, query: next })
  }

  async function load() {
    loading.value = true
    try {
      result.value = await service.search(query.value)
    } catch (err) {
      notifyError(err)
    } finally {
      loading.value = false
    }
  }

  watch(query, load, { immediate: true, deep: true })
  return { query, result, loading, update, reload: load }
}
