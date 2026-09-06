import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { mount } from '@vue/test-utils'
import CategoryPicker from '@/components/upload/CategoryPicker.vue'
import { useCategoryStore } from '@/stores/category-store'
import { i18n } from '@/boot/i18n'
import type { CategoryNode } from '@/utils/models/catalog-models'

const mocks = vi.hoisted(() => ({ getCategories: vi.fn() }))
vi.mock('@/utils/services/catalog-service', () => ({
  CatalogService: class {
    getCategories = mocks.getCategories
  },
}))

const node = (id: number, slug: string, level: number, children: CategoryNode[] = []): CategoryNode =>
  ({ id, slug, nameEn: slug, nameTr: `${slug}-tr`, icon: null, level, parentId: null, appCount: 0, children }) as CategoryNode

const tree = [
  node(1, 'system', 1, [node(2, 'hardware', 2, [node(3, 'monitoring', 3), node(4, 'sysinfo', 3)]), node(5, 'files', 2)]),
  node(6, 'games', 1, [node(7, 'puzzle', 2)]),
]

async function render(modelValue: number | null, error?: string) {
  const pinia = createPinia()
  setActivePinia(pinia)
  mocks.getCategories.mockResolvedValue(tree)
  await useCategoryStore().fetchTree()
  const w = mount(CategoryPicker, { props: { modelValue, error }, global: { plugins: [pinia, i18n] } })
  await w.vm.$nextTick()
  return w
}

const labels = (w: Awaited<ReturnType<typeof render>>) => w.findAll('.picker__label').map((l) => l.text())

describe('CategoryPicker', () => {
  beforeEach(() => {
    i18n.global.locale.value = 'en-US'
  })

  it('starts with a single required category select', async () => {
    const w = await render(null)
    expect(labels(w)).toEqual(['Category*'])
    expect(w.text()).toContain('Choose the category and the sub-category')
  })

  it('renders one select per level of the chosen path plus the next level', async () => {
    expect(labels(await render(1))).toEqual(['Category*', 'Sub-category*'])
    expect(labels(await render(2))).toEqual(['Category*', 'Sub-category*', 'Type (optional)'])
    expect(labels(await render(3))).toEqual(['Category*', 'Sub-category*', 'Type (optional)'])
    expect(labels(await render(5))).toEqual(['Category*', 'Sub-category*'])
  })

  it('emits the chosen id and falls back to the parent when a level is cleared', async () => {
    const w = await render(3)
    const selects = w.findAllComponents({ name: 'ElSelect' })
    expect(selects).toHaveLength(3)
    selects[1]!.vm.$emit('update:modelValue', 5)
    expect(w.emitted('update:modelValue')?.at(-1)).toEqual([5])
    selects[2]!.vm.$emit('update:modelValue', null)
    expect(w.emitted('update:modelValue')?.at(-1)).toEqual([2])
    selects[0]!.vm.$emit('update:modelValue', 6)
    expect(w.emitted('update:modelValue')?.at(-1)).toEqual([6])
  })

  it('shows the server error instead of the hint', async () => {
    const w = await render(1, 'Pick a category and a sub-category.')
    expect(w.find('.picker__hint').text()).toBe('Pick a category and a sub-category.')
    expect(w.classes()).toContain('is-error')
  })

  it('uses Turkish labels and names when the locale is tr-TR', async () => {
    i18n.global.locale.value = 'tr-TR'
    const w = await render(1)
    expect(labels(w)).toEqual(['Kategori*', 'Alt kategori*'])
    expect(w.text()).toContain('system-tr')
  })
})
