import enUS from './en-US'
import type { AppLocale } from './locales'

export type Messages = Record<string, string>

/** English is bundled (fallback); every other language is a separate chunk fetched the first time it is selected. */
export const bundledMessages: Record<string, Messages> = { 'en-US': enUS }

export const loaders: Record<AppLocale, () => Promise<{ default: Messages }>> = {
  'en-US': () => Promise.resolve({ default: enUS }),
  'tr-TR': () => import('./tr-TR'),
  'es-ES': () => import('./es-ES'),
  'pt-BR': () => import('./pt-BR'),
  'de-DE': () => import('./de-DE'),
  'fr-FR': () => import('./fr-FR'),
  'ar-SA': () => import('./ar-SA'),
  'ru-RU': () => import('./ru-RU'),
  'ja-JP': () => import('./ja-JP'),
  'ko-KR': () => import('./ko-KR'),
  'zh-CN': () => import('./zh-CN'),
}
