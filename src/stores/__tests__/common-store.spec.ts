import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useCommonStore } from '@/stores/common-store'

describe('common store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    document.head.querySelectorAll('meta').forEach((m) => m.remove())
    vi.stubEnv('VITE_APP_TITLE', '')
  })

  it('sets the document title with the site suffix', () => {
    const store = useCommonStore()
    store.setPageTitle('Search')
    expect(store.pageTitle).toBe('Search')
    expect(document.title).toBe('Search · AI Already Did It')
    store.setPageTitle('')
    expect(document.title).toBe('AI Already Did It')
    vi.stubEnv('VITE_APP_TITLE', 'Custom')
    store.setPageTitle('Home')
    expect(document.title).toBe('Home · Custom')
  })

  it('creates and updates meta tags', () => {
    const store = useCommonStore()
    store.setPageTitle('App', 'first description')
    const meta = document.head.querySelector<HTMLMetaElement>('meta[name="description"]')
    expect(meta?.content).toBe('first description')
    store.setMeta('description', 'second')
    expect(document.head.querySelectorAll('meta[name="description"]')).toHaveLength(1)
    expect(meta?.content).toBe('second')
  })

  it('keeps breadcrumb and active menu', () => {
    const store = useCommonStore()
    store.setBreadcrumb([{ label: 'Home', to: '/' }, { label: 'Apps' }])
    expect(store.breadcrumb).toHaveLength(2)
    store.setActiveMenu({ label: 'adm_apps', route: '/admin/apps' })
    expect(store.activeMenu?.route).toBe('/admin/apps')
    store.setActiveMenu(null)
    expect(store.activeMenu).toBeNull()
  })
})
