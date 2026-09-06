import { beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick, reactive } from 'vue'
import { useAppBrowser } from '@/pages/catalog/use-app-browser'
import type { AppSearchResult } from '@/utils/models/catalog-models'

const mocks = vi.hoisted(() => ({
  route: { path: '/search', query: {} as Record<string, string> },
  push: vi.fn(),
  search: vi.fn(),
  notifyError: vi.fn(),
}))
vi.mock('vue-router', () => ({ useRoute: () => mocks.route, useRouter: () => ({ push: mocks.push }) }))
vi.mock('@/utils/services/catalog-service', () => ({
  CatalogService: class {
    search = mocks.search
  },
}))
vi.mock('@/utils/tools', () => ({ notifyError: mocks.notifyError }))

const result = (n: number): AppSearchResult => ({ page: { items: [], page: 1, pageSize: 24, totalCount: n, totalPages: 1 }, platforms: [], licenses: [], models: [], categories: [], modeUsed: 'hybrid', semanticAvailable: true })
const flush = () => new Promise((r) => setTimeout(r, 0))

describe('useAppBrowser', () => {
  beforeEach(() => {
    mocks.route = reactive({ path: '/search', query: {} as Record<string, string> })
    mocks.push.mockReset()
    mocks.search.mockReset()
    mocks.search.mockResolvedValue(result(1))
    mocks.notifyError.mockReset()
  })

  it('parses the URL query into a typed AppQuery and loads immediately', async () => {
    mocks.route.query = { q: 'cpu', mode: 'semantic', minRating: '70', page: '3', tags: 'gtk', bogus: 'x' }
    const { query, result: res, loading } = useAppBrowser(() => ({}))
    expect(query.value).toMatchObject({ q: 'cpu', mode: 'semantic', minRating: 70, page: 3, tags: 'gtk', pageSize: 24 })
    expect('bogus' in query.value).toBe(false)
    expect(loading.value).toBe(true)
    await flush()
    expect(loading.value).toBe(false)
    expect(mocks.search).toHaveBeenCalledWith(query.value)
    expect(res.value.page.totalCount).toBe(1)
  })

  it('applies fixed filters and re-fetches when the route changes', async () => {
    const { query } = useAppBrowser(() => ({ category: 'system' }))
    expect(query.value.category).toBe('system')
    expect(query.value.page).toBe(1)
    await flush()
    expect(mocks.search).toHaveBeenCalledTimes(1)
    mocks.route.query = { platform: 'linux' }
    await nextTick()
    await flush()
    expect(mocks.search).toHaveBeenCalledTimes(2)
    expect(mocks.search).toHaveBeenLastCalledWith(expect.objectContaining({ platform: 'linux', category: 'system' }))
  })

  it('update() pushes a cleaned URL without page 1, pageSize or fixed keys', async () => {
    mocks.route.query = { q: 'cpu', page: '2' }
    const { update } = useAppBrowser(() => ({ category: 'system' }))
    await flush()
    update({ platform: 'linux', page: 1, license: '' })
    expect(mocks.push).toHaveBeenCalledWith({ path: '/search', query: { q: 'cpu', platform: 'linux' } })
    update({ page: 4, sort: 'newest' })
    expect(mocks.push).toHaveBeenLastCalledWith({ path: '/search', query: { q: 'cpu', page: '4', sort: 'newest' } })
  })

  it('reports fetch errors and keeps the previous result', async () => {
    const { result: res, reload } = useAppBrowser(() => ({}))
    await flush()
    mocks.search.mockRejectedValueOnce(new Error('down'))
    await reload()
    expect(mocks.notifyError).toHaveBeenCalledTimes(1)
    expect(res.value.page.totalCount).toBe(1)
  })
})
