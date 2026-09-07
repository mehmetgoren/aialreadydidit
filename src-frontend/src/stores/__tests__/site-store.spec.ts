import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useSiteStore } from '@/stores/site-store'
import type { SiteConfig } from '@/utils/models/site-models'

const mocks = vi.hoisted(() => ({ config: vi.fn() }))
vi.mock('@/utils/services/site-service', () => ({
  SiteService: class {
    config = mocks.config
  },
}))

const cfg = (patch: Partial<SiteConfig> = {}): SiteConfig =>
  ({
    siteName: 'AADI',
    googleClientId: null,
    semanticSearchAvailable: true,
    uploadLimits: { minScreenshots: 1 },
    platforms: [
      { id: 1, code: 'linux', name: 'Linux' },
      { id: 2, code: 'web', name: 'Web' },
    ],
    licenses: [{ id: 1, spdxId: 'MIT' }],
    llmModels: [],
    ...patch,
  }) as unknown as SiteConfig

describe('site store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    mocks.config.mockReset()
  })

  it('is empty before loading', () => {
    const store = useSiteStore()
    expect(store.platforms).toEqual([])
    expect(store.licenses).toEqual([])
    expect(store.limits).toBeNull()
    expect(store.semantic).toBe(false)
    expect(store.googleClientId).toBeNull()
    expect(store.platformName('linux')).toBe('linux')
    expect(store.platformName(null)).toBe('')
  })

  it('loads once and de-duplicates concurrent calls', async () => {
    let resolve!: (c: SiteConfig) => void
    mocks.config.mockReturnValue(new Promise<SiteConfig>((r) => (resolve = r)))
    const store = useSiteStore()
    const p1 = store.ensureLoaded()
    const p2 = store.ensureLoaded()
    expect(store.loading).toBe(true)
    resolve(cfg({ googleClientId: 'g-123' }))
    await Promise.all([p1, p2])
    expect(mocks.config).toHaveBeenCalledTimes(1)
    expect(store.loading).toBe(false)
    expect(store.semantic).toBe(true)
    expect(store.googleClientId).toBe('g-123')
    expect(store.platformName('web')).toBe('Web')
    expect(store.platformName('amiga')).toBe('amiga')

    await store.ensureLoaded()
    expect(mocks.config).toHaveBeenCalledTimes(1)
    mocks.config.mockResolvedValue(cfg({ siteName: 'Fresh' }))
    await store.ensureLoaded(true)
    expect(mocks.config).toHaveBeenCalledTimes(2)
    expect(store.config?.siteName).toBe('Fresh')
  })

  it('swallows load failures and allows a retry', async () => {
    mocks.config.mockRejectedValueOnce(new Error('down'))
    const store = useSiteStore()
    expect(await store.ensureLoaded()).toBeNull()
    expect(store.loading).toBe(false)
    mocks.config.mockResolvedValueOnce(cfg())
    expect((await store.ensureLoaded())?.siteName).toBe('AADI')
  })
})
