import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

export interface BreadcrumbItem {
  label: string
  to?: string
}

/** Page chrome shared by layouts: title, breadcrumb, active admin menu, SEO meta. */
export const useCommonStore = defineStore('common', () => {
  const _breadcrumb = ref<BreadcrumbItem[]>([])
  const _pageTitle = ref<string>('')
  const _activeMenu = ref<{ label: string; icon?: string; route?: string } | null>(null)

  const breadcrumb = computed(() => _breadcrumb.value)
  const pageTitle = computed(() => _pageTitle.value)
  const activeMenu = computed(() => _activeMenu.value)

  function setBreadcrumb(items: BreadcrumbItem[]) {
    _breadcrumb.value = items
  }
  function setPageTitle(title: string, description?: string) {
    _pageTitle.value = title
    const siteName = import.meta.env.VITE_APP_TITLE || 'AI Already Did It'
    document.title = title ? `${title} · ${siteName}` : siteName
    if (description !== undefined) setMeta('description', description)
  }
  function setMeta(name: string, content: string) {
    let el = document.querySelector<HTMLMetaElement>(`meta[name="${name}"]`)
    if (!el) {
      el = document.createElement('meta')
      el.name = name
      document.head.appendChild(el)
    }
    el.content = content
  }
  function setActiveMenu(item: { label: string; icon?: string; route?: string } | null) {
    _activeMenu.value = item
  }

  return { breadcrumb, pageTitle, activeMenu, setBreadcrumb, setPageTitle, setMeta, setActiveMenu }
})
