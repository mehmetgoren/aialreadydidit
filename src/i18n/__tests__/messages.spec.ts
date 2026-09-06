import { describe, expect, it } from 'vitest'
import messages from '@/i18n/all'
import enCommon from '@/i18n/en-US/common'
import enCatalog from '@/i18n/en-US/catalog'
import enDashboard from '@/i18n/en-US/dashboard'
import enUpload from '@/i18n/en-US/upload'
import enAdmin from '@/i18n/en-US/admin'
import trCommon from '@/i18n/tr-TR/common'
import trCatalog from '@/i18n/tr-TR/catalog'
import trDashboard from '@/i18n/tr-TR/dashboard'
import trUpload from '@/i18n/tr-TR/upload'
import trAdmin from '@/i18n/tr-TR/admin'

type Dict = Record<string, string>
const en = messages['en-US'] as Dict
const tr = messages['tr-TR'] as Dict
const others = Object.entries(messages).filter(([code]) => code !== 'en-US') as [string, Dict][]
// Unique names: plural forms ('{n} rating | {n} ratings') repeat a placeholder that Turkish (no plural split) uses once.
const placeholders = (s: string) => [...new Set([...s.matchAll(/\{(\w+)\}/g)].map((m) => m[1]))].sort()

describe('i18n messages', () => {
  it('every locale exposes exactly the same keys as en-US', () => {
    const enKeys = Object.keys(en).sort()
    expect(enKeys.length).toBeGreaterThan(600)
    expect(others.map(([code]) => code).sort()).toEqual(['ar-SA', 'de-DE', 'es-ES', 'fr-FR', 'ja-JP', 'ko-KR', 'pt-BR', 'ru-RU', 'tr-TR', 'zh-CN'])
    for (const [code, dict] of others) {
      const keys = Object.keys(dict).sort()
      expect(enKeys.filter((k) => !(k in dict)), `keys missing in ${code}`).toEqual([])
      expect(keys.filter((k) => !(k in en)), `extra keys in ${code}`).toEqual([])
    }
  })

  it('every value is a non-empty string and every key is a flat identifier', () => {
    for (const [locale, dict] of Object.entries(messages)) {
      for (const [key, value] of Object.entries(dict)) {
        expect(typeof value, `${locale}.${key}`).toBe('string')
        expect(value.trim().length, `${locale}.${key} is empty`).toBeGreaterThan(0)
        // status_* keys embed the API enum value (e.g. status_pendingScan), hence the camelCase tail.
        expect(key, `${locale}.${key} not snake_case`).toMatch(/^[a-z][a-zA-Z0-9_]*$/)
      }
    }
  })

  it('interpolation placeholders match en-US in every locale', () => {
    for (const [code, dict] of others) {
      const mismatched = Object.keys(en).filter((k) => dict[k] !== undefined && placeholders(en[k]!).join() !== placeholders(dict[k]!).join())
      expect(mismatched, `placeholder mismatch in ${code}`).toEqual([])
    }
  })

  it('translations are not just copies of the English text', () => {
    // Technical labels legitimately stay identical (API, MCP, URL…); a real translation differs on most keys.
    for (const [code, dict] of others) {
      const same = Object.keys(en).filter((k) => dict[k] === en[k]).length
      expect(same / Object.keys(en).length, `${code} looks untranslated`).toBeLessThan(0.35)
    }
  })

  it('domain files never shadow each other', () => {
    for (const [locale, files] of Object.entries({ en: [enCommon, enCatalog, enDashboard, enUpload, enAdmin], tr: [trCommon, trCatalog, trDashboard, trUpload, trAdmin] })) {
      const seen = new Map<string, number>()
      files.forEach((file, i) => {
        for (const key of Object.keys(file)) {
          expect(seen.has(key), `${locale}: key "${key}" defined in file #${seen.get(key)} and #${i}`).toBe(false)
          seen.set(key, i)
        }
      })
      expect(seen.size).toBe(Object.keys(locale === 'en' ? en : tr).length)
    }
  })

  it('keys referenced by StatusTag, dashboard menu and validation exist', () => {
    const needed = ['status_published', 'status_pendingReview', 'status_rejected', 'status_infected', 'dash_overview', 'dash_api_keys', 'v_required', 'v_invalid_email', 'error_generic', 'copied', 'copy_failed', 'confirm', 'yes', 'cancel', 'ok', 'input', 'loading']
    expect(needed.filter((k) => !(k in en))).toEqual([])
  })
})
