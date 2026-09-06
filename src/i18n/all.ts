import enUS from './en-US'
import trTR from './tr-TR'
import esES from './es-ES'
import ptBR from './pt-BR'
import deDE from './de-DE'
import frFR from './fr-FR'
import arSA from './ar-SA'
import ruRU from './ru-RU'
import jaJP from './ja-JP'
import koKR from './ko-KR'
import zhCN from './zh-CN'
import type { AppLocale } from './locales'
import type { Messages } from './index'

/** Every language eagerly — for tests and tooling only; the app loads languages lazily through `loaders`. */
const all: Record<AppLocale, Messages> = {
  'en-US': enUS,
  'tr-TR': trTR,
  'es-ES': esES,
  'pt-BR': ptBR,
  'de-DE': deDE,
  'fr-FR': frFR,
  'ar-SA': arSA,
  'ru-RU': ruRU,
  'ja-JP': jaJP,
  'ko-KR': koKR,
  'zh-CN': zhCN,
}

export default all
