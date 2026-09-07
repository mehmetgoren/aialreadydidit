import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { CategoryNode } from '@/utils/models/catalog-models'
import { CatalogService } from '@/utils/services/catalog-service'

export const useCategoryStore = defineStore('category', () => {
  const _tree = ref<CategoryNode[]>([])
  const _loaded = ref(false)
  const _loading = ref(false)

  const tree = computed(() => _tree.value)
  const roots = computed(() => _tree.value)
  const loaded = computed(() => _loaded.value)

  const bySlug = computed(() => {
    const map = new Map<string, CategoryNode>()
    const walk = (nodes: CategoryNode[]) => {
      for (const n of nodes) {
        map.set(n.slug, n)
        walk(n.children)
      }
    }
    walk(_tree.value)
    return map
  })

  const byId = computed(() => {
    const map = new Map<number, CategoryNode>()
    for (const n of bySlug.value.values()) map.set(n.id, n)
    return map
  })

  async function fetchTree(force = false) {
    if ((_loaded.value && !force) || _loading.value) return _tree.value
    _loading.value = true
    try {
      _tree.value = await new CatalogService().getCategories()
      _loaded.value = true
    } finally {
      _loading.value = false
    }
    return _tree.value
  }

  /** Root → … → node path for breadcrumbs. */
  function pathTo(slug: string): CategoryNode[] {
    const path: CategoryNode[] = []
    const walk = (nodes: CategoryNode[]): boolean => {
      for (const n of nodes) {
        path.push(n)
        if (n.slug === slug || walk(n.children)) return true
        path.pop()
      }
      return false
    }
    walk(_tree.value)
    return path
  }

  function pathToId(id: number): CategoryNode[] {
    const node = byId.value.get(id)
    return node ? pathTo(node.slug) : []
  }

  return { tree, roots, loaded, bySlug, byId, fetchTree, pathTo, pathToId }
})

/** Localised category name (en / tr) — small composable used by templates. */
export function useCategoryName() {
  const { locale } = useI18n()
  return (node: { nameEn: string; nameTr: string } | null | undefined) =>
    !node ? '' : locale.value === 'tr-TR' ? node.nameTr : node.nameEn
}
