import { createI18n } from 'vue-i18n'
import { bundledMessages, loaders } from '@/i18n'
import { DEFAULT_LOCALE, LOCALES, detectLocale, isAppLocale, localeDef, type AppLocale } from '@/i18n/locales'
import { localService } from '@/utils/services/local-service'

export type { AppLocale } from '@/i18n/locales'
export const SUPPORTED_LOCALES: { value: AppLocale; label: string; short: string }[] = LOCALES.map((l) => ({ value: l.code, label: l.label, short: l.short }))

function initialLocale(): AppLocale {
  const stored = localService.getLanguage()
  if (isAppLocale(stored)) return stored
  // Global store: English by default; otherwise the browser's preferred language when we have it.
  return detectLocale(navigator.languages?.length ? navigator.languages : [navigator.language])
}

export const i18n = createI18n({
  legacy: false,
  locale: initialLocale(),
  fallbackLocale: DEFAULT_LOCALE,
  messages: bundledMessages,
  missingWarn: false,
  fallbackWarn: false,
})

/** Mirrors the active locale onto <html lang dir> (Arabic is right-to-left). */
export function applyDocumentLocale(locale: string) {
  const def = localeDef(locale)
  document.documentElement.lang = def.code.slice(0, 2)
  document.documentElement.dir = def.dir
}

/** Fetches a language chunk once and registers it; English is always present. */
export async function loadLocaleMessages(locale: AppLocale) {
  if (i18n.global.availableLocales.includes(locale)) return
  const mod = await loaders[locale]()
  i18n.global.setLocaleMessage(locale, mod.default)
}

export async function setLocale(locale: AppLocale) {
  await loadLocaleMessages(locale)
  i18n.global.locale.value = locale
  localService.setLanguage(locale)
  applyDocumentLocale(locale)
}
