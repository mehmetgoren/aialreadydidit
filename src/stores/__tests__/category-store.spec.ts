import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { defineComponent, h } from 'vue'
import { mount } from '@vue/test-utils'
import { useCategoryName, useCategoryStore } from '@/stores/category-store'
import { i18n } from '@/boot/i18n'
import type { CategoryNode } from '@/utils/models/catalog-models'

const mocks = vi.hoisted(() => ({ getCategories: vi.fn() }))
vi.mock('@/utils/services/catalog-service', () => ({
  CatalogService: class {
    getCategories = mocks.getCategories
  },
}))

const node = (id: number, slug: string, children: CategoryNode[] = []): CategoryNode =>
  ({ id, slug, nameEn: `${slug}-en`, nameTr: `${slug}-tr`, icon: null, level: 0, parentId: null, appCount: 0, children }) as CategoryNode

const tree: CategoryNode[] = [
  node(1, 'system', [node(2, 'hardware', [node(3, 'monitoring'), node(4, 'sysinfo')]), node(5, 'files')]),
  node(6, 'developer-tools', [node(7, 'cli')]),
]

describe('category store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    mocks.getCategories.mockReset()
    mocks.getCategories.mockResolvedValue(tree)
  })

  it('fetches the tree once unless forced', async () => {
    const store = useCategoryStore()
    expect(store.loaded).toBe(false)
    await store.fetchTree()
    await store.fetchTree()
    expect(mocks.getCategories).toHaveBeenCalledTimes(1)
    expect(store.roots.map((n) => n.slug)).toEqual(['system', 'developer-tools'])
    await store.fetchTree(true)
    expect(mocks.getCategories).toHaveBeenCalledTimes(2)
  })

  it('indexes every node by slug and id', async () => {
    const store = useCategoryStore()
    await store.fetchTree()
    expect(store.bySlug.size).toBe(7)
    expect(store.bySlug.get('monitoring')?.id).toBe(3)
    expect(store.byId.get(7)?.slug).toBe('cli')
  })

  it('builds breadcrumb paths', async () => {
    const store = useCategoryStore()
    await store.fetchTree()
    expect(store.pathTo('monitoring').map((n) => n.slug)).toEqual(['system', 'hardware', 'monitoring'])
    expect(store.pathTo('system').map((n) => n.slug)).toEqual(['system'])
    expect(store.pathTo('nope')).toEqual([])
    expect(store.pathToId(5).map((n) => n.slug)).toEqual(['system', 'files'])
    expect(store.pathToId(99)).toEqual([])
  })

  it('resets loading when the API fails', async () => {
    mocks.getCategories.mockRejectedValueOnce(new Error('down'))
    const store = useCategoryStore()
    await expect(store.fetchTree()).rejects.toThrow('down')
    expect(store.loaded).toBe(false)
    await store.fetchTree()
    expect(store.loaded).toBe(true)
  })
})

describe('useCategoryName', () => {
  it('picks the name for the active locale', async () => {
    let name!: ReturnType<typeof useCategoryName>
    const Probe = defineComponent({
      setup() {
        name = useCategoryName()
        return () => h('div')
      },
    })
    mount(Probe, { global: { plugins: [i18n] } })
    const n = node(1, 'system')
    expect(name(n)).toBe('system-en')
    expect(name(null)).toBe('')
    i18n.global.locale.value = 'tr-TR'
    expect(name(n)).toBe('system-tr')
    i18n.global.locale.value = 'en-US'
  })
})
