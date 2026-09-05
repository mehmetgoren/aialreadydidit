import { createI18n } from 'vue-i18n'
import messages from '@/i18n'
import { localService } from '@/utils/services/local-service'

export type AppLocale = 'en-US' | 'tr-TR'
export const SUPPORTED_LOCALES: { value: AppLocale; label: string }[] = [
  { value: 'en-US', label: 'English' },
  { value: 'tr-TR', label: 'Türkçe' },
]

function initialLocale(): AppLocale {
  const stored = localService.getLanguage()
  if (stored === 'tr-TR' || stored === 'en-US') return stored
  // Global store: English by default; browsers in Turkish get Turkish.
  return navigator.language?.toLowerCase().startsWith('tr') ? 'tr-TR' : 'en-US'
}

export const i18n = createI18n({
  legacy: false,
  locale: initialLocale(),
  fallbackLocale: 'en-US',
  messages,
  missingWarn: false,
  fallbackWarn: false,
})

export function setLocale(locale: AppLocale) {
  i18n.global.locale.value = locale
  localService.setLanguage(locale)
  document.documentElement.lang = locale.slice(0, 2)
}
