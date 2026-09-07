import { describe, expect, it } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'
import { routes } from '@/router/routes'
import { DASHBOARD_MENU } from '@/utils/dashboard-menu'
import messages from '@/i18n/all'

type Flat = { path: string; name?: string; meta: RouteRecordRaw['meta'] }

function flatten(records: RouteRecordRaw[], prefix = '', inherited: RouteRecordRaw['meta'] = {}): Flat[] {
  const out: Flat[] = []
  for (const r of records) {
    const raw = r.path.startsWith('/') ? r.path : `${prefix}/${r.path}`
    const path = raw.replace(/\/+/g, '/').replace(/(.)\/$/, '$1')
    const meta = { ...inherited, ...(r.meta ?? {}) }
    if (r.children?.length) out.push(...flatten(r.children, path, meta))
    else out.push({ path, name: r.name as string | undefined, meta })
  }
  return out
}

const leaves = flatten(routes)
const en = messages['en-US'] as Record<string, string>

describe('route table', () => {
  it('route names are unique', () => {
    const names = leaves.map((l) => l.name).filter(Boolean) as string[]
    expect(new Set(names).size).toBe(names.length)
  })

  it('storefront and account pages are public, dashboard / upload / admin are not', () => {
    for (const l of leaves) {
      const p = l.path
      if (p.startsWith('/dashboard') || p.startsWith('/upload') || p.startsWith('/admin')) {
        expect(l.meta?.public, `${p} must not be public`).not.toBe(true)
      } else {
        expect(l.meta?.public, `${p} must be public`).toBe(true)
      }
      if (p.startsWith('/admin')) expect(l.meta?.requiresAdmin, `${p} must require admin`).toBe(true)
      else expect(l.meta?.requiresAdmin, `${p} must not require admin`).not.toBe(true)
    }
  })

  it('every page has a titleKey that exists in the messages', () => {
    for (const l of leaves) {
      if (l.name === 'not-found') continue
      expect(l.meta?.titleKey, `${l.path} has no titleKey`).toBeTruthy()
      expect(en[l.meta!.titleKey!], `${l.path}: missing i18n key ${l.meta!.titleKey}`).toBeTruthy()
    }
  })

  it('dashboard menu entries point at existing routes with existing labels', () => {
    const paths = new Set(leaves.map((l) => l.path))
    for (const item of DASHBOARD_MENU) {
      expect(paths.has(item.route!), `menu route ${item.route} not in routes`).toBe(true)
      expect(en[item.label], `menu label ${item.label} missing`).toBeTruthy()
    }
  })
})
