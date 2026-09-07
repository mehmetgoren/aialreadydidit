/**
 * Single registry of the UI languages. Everything locale-aware (vue-i18n, dayjs, Intl, Element Plus, text direction,
 * browser-language detection) derives from this list — add a language here and in `src/i18n/<code>/`.
 */
export const LOCALE_CODES = ['en-US', 'tr-TR', 'es-ES', 'pt-BR', 'de-DE', 'fr-FR', 'ar-SA', 'ru-RU', 'ja-JP', 'ko-KR', 'zh-CN'] as const
export type AppLocale = (typeof LOCALE_CODES)[number]

export interface LocaleDef {
  code: AppLocale
  /** Native name shown in the switcher. */
  label: string
  /** Two-letter badge shown in the header. */
  short: string
  /** dayjs locale id (`dayjs/locale/<id>`). */
  dayjs: string
  /** BCP 47 tag for Intl formatting (Arabic keeps Western digits via `nu-latn`). */
  intl: string
  dir: 'ltr' | 'rtl'
  /** navigator.language prefixes that map to this locale. */
  match: string[]
}

export const LOCALES: LocaleDef[] = [
  { code: 'en-US', label: 'English', short: 'EN', dayjs: 'en', intl: 'en-US', dir: 'ltr', match: ['en'] },
  { code: 'tr-TR', label: 'Türkçe', short: 'TR', dayjs: 'tr', intl: 'tr-TR', dir: 'ltr', match: ['tr'] },
  { code: 'es-ES', label: 'Español', short: 'ES', dayjs: 'es', intl: 'es-ES', dir: 'ltr', match: ['es'] },
  { code: 'pt-BR', label: 'Português (Brasil)', short: 'PT', dayjs: 'pt-br', intl: 'pt-BR', dir: 'ltr', match: ['pt'] },
  { code: 'de-DE', label: 'Deutsch', short: 'DE', dayjs: 'de', intl: 'de-DE', dir: 'ltr', match: ['de'] },
  { code: 'fr-FR', label: 'Français', short: 'FR', dayjs: 'fr', intl: 'fr-FR', dir: 'ltr', match: ['fr'] },
  { code: 'ar-SA', label: 'العربية', short: 'AR', dayjs: 'ar', intl: 'ar-u-nu-latn', dir: 'rtl', match: ['ar'] },
  { code: 'ru-RU', label: 'Русский', short: 'RU', dayjs: 'ru', intl: 'ru-RU', dir: 'ltr', match: ['ru'] },
  { code: 'ja-JP', label: '日本語', short: 'JA', dayjs: 'ja', intl: 'ja-JP', dir: 'ltr', match: ['ja'] },
  { code: 'ko-KR', label: '한국어', short: 'KO', dayjs: 'ko', intl: 'ko-KR', dir: 'ltr', match: ['ko'] },
  { code: 'zh-CN', label: '简体中文', short: 'ZH', dayjs: 'zh-cn', intl: 'zh-CN', dir: 'ltr', match: ['zh'] },
]

export const DEFAULT_LOCALE: AppLocale = 'en-US'

export function isAppLocale(value: unknown): value is AppLocale {
  return typeof value === 'string' && (LOCALE_CODES as readonly string[]).includes(value)
}

export function localeDef(code: string): LocaleDef {
  return LOCALES.find((l) => l.code === code) ?? LOCALES[0]!
}

/** Best locale for a list of browser languages (`navigator.languages`), e.g. ["pt-PT", "en"] → pt-BR. */
export function detectLocale(languages: readonly string[] | undefined | null): AppLocale {
  for (const lang of languages ?? []) {
    const lower = lang.toLowerCase()
    const exact = LOCALES.find((l) => l.code.toLowerCase() === lower)
    if (exact) return exact.code
    const prefix = lower.split(/[-_]/)[0]!
    const byPrefix = LOCALES.find((l) => l.match.includes(prefix))
    if (byPrefix) return byPrefix.code
  }
  return DEFAULT_LOCALE
}
