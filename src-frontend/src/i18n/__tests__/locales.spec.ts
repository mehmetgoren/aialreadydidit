import { describe, expect, it } from 'vitest'
import { DEFAULT_LOCALE, LOCALES, LOCALE_CODES, detectLocale, isAppLocale, localeDef } from '@/i18n/locales'

describe('locale registry', () => {
  it('lists every code once with the fields the app relies on', () => {
    expect(LOCALES.map((l) => l.code)).toEqual([...LOCALE_CODES])
    expect(new Set(LOCALE_CODES).size).toBe(LOCALE_CODES.length)
    for (const l of LOCALES) {
      expect(l.label.length).toBeGreaterThan(1)
      expect(l.short).toMatch(/^[A-Z]{2}$/)
      expect(l.match.length).toBeGreaterThan(0)
      expect(() => new Intl.NumberFormat(l.intl).format(1234.5)).not.toThrow()
    }
    expect(localeDef('ar-SA').dir).toBe('rtl')
    expect(LOCALES.filter((l) => l.dir === 'rtl').map((l) => l.code)).toEqual(['ar-SA'])
  })

  it('detects the best locale from browser languages', () => {
    expect(detectLocale(['pt-PT', 'en'])).toBe('pt-BR')
    expect(detectLocale(['zh-TW'])).toBe('zh-CN')
    expect(detectLocale(['fr-CA'])).toBe('fr-FR')
    expect(detectLocale(['nl', 'de'])).toBe('de-DE')
    expect(detectLocale(['ja'])).toBe('ja-JP')
    expect(detectLocale(['nl-NL'])).toBe(DEFAULT_LOCALE)
    expect(detectLocale([])).toBe(DEFAULT_LOCALE)
    expect(detectLocale(undefined)).toBe(DEFAULT_LOCALE)
  })

  it('isAppLocale / localeDef', () => {
    expect(isAppLocale('ko-KR')).toBe(true)
    expect(isAppLocale('ko')).toBe(false)
    expect(isAppLocale(null)).toBe(false)
    expect(localeDef('nope').code).toBe('en-US')
    expect(localeDef('ar-SA').intl).toBe('ar-u-nu-latn')
  })
})

describe('lazy locale loading', () => {
  it('registers a language chunk once and switches to it', async () => {
    const { i18n, loadLocaleMessages, setLocale } = await import('@/boot/i18n')
    expect(i18n.global.availableLocales).toContain('en-US')
    await setLocale('de-DE')
    expect(i18n.global.locale.value).toBe('de-DE')
    expect(i18n.global.t('all_categories')).toBe('Alle Kategorien')
    expect(document.documentElement.lang).toBe('de')
    await setLocale('ar-SA')
    expect(document.documentElement.dir).toBe('rtl')
    await loadLocaleMessages('ar-SA') // idempotent
    await setLocale('en-US')
    expect(document.documentElement.dir).toBe('ltr')
  })
})
