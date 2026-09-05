import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import type { SiteConfig } from '@/utils/models/site-models'
import { SiteService } from '@/utils/services/site-service'

/** Boot configuration from GET /site/config (platforms, licenses, models, limits, feature flags). */
export const useSiteStore = defineStore('site', () => {
  const config = ref<SiteConfig | null>(null)
  const loading = ref(false)
  let pending: Promise<SiteConfig | null> | null = null

  const platforms = computed(() => config.value?.platforms ?? [])
  const licenses = computed(() => config.value?.licenses ?? [])
  const llmModels = computed(() => config.value?.llmModels ?? [])
  const limits = computed(() => config.value?.uploadLimits ?? null)
  const semantic = computed(() => config.value?.semanticSearchAvailable ?? false)
  const googleClientId = computed(() => config.value?.googleClientId ?? null)

  async function ensureLoaded(force = false) {
    if (config.value && !force) return config.value
    if (pending) return pending
    loading.value = true
    pending = new SiteService()
      .config()
      .then((c) => (config.value = c))
      .catch(() => null)
      .finally(() => {
        loading.value = false
        pending = null
      })
    return pending
  }

  function platformName(code: string | null | undefined): string {
    if (!code) return ''
    return platforms.value.find((p) => p.code === code)?.name ?? code
  }

  return { config, loading, platforms, licenses, llmModels, limits, semantic, googleClientId, ensureLoaded, platformName }
})
