import { afterEach, describe, expect, it } from 'vitest'
import { i18n } from '@/boot/i18n'
import { formatBytes, formatCompact, formatDate, formatDateTime, formatMoney, formatNumber, formatPercent, formatScore, fromNow, toApiDate } from '@/utils/format'

describe('format', () => {
  afterEach(() => {
    i18n.global.locale.value = 'en-US'
  })

  it('formats dates in the active locale', () => {
    expect(formatDate('2026-09-06T10:30:00')).toBe('06 Sep 2026')
    expect(formatDateTime('2026-09-06T10:30:00')).toBe('06 Sep 2026 10:30')
    expect(formatDate(null)).toBe('-')
    expect(formatDate(undefined)).toBe('-')
    i18n.global.locale.value = 'tr-TR'
    expect(formatDate('2026-09-06T10:30:00')).toBe('06 Eyl 2026')
  })

  it('fromNow is empty for missing values and localised otherwise', () => {
    expect(fromNow(null)).toBe('')
    expect(fromNow(new Date())).toMatch(/seconds|now/)
    i18n.global.locale.value = 'tr-TR'
    expect(fromNow(new Date(Date.now() - 3600_000))).toContain('saat')
  })

  it('toApiDate yields YYYY-MM-DD', () => {
    expect(toApiDate(new Date(2026, 8, 6))).toBe('2026-09-06')
    expect(toApiDate('2026-01-31T23:00:00')).toBe('2026-01-31')
    expect(toApiDate(null)).toBeUndefined()
  })

  it('formatNumber respects digits and locale', () => {
    expect(formatNumber(1234.5)).toBe('1,235')
    expect(formatNumber(1234.5, 1)).toBe('1,234.5')
    expect(formatNumber(null)).toBe('-')
    expect(formatNumber(Number.NaN)).toBe('-')
    i18n.global.locale.value = 'tr-TR'
    expect(formatNumber(1234.5, 1)).toBe('1.234,5')
  })

  it('formatCompact abbreviates thousands and millions', () => {
    expect(formatCompact(12345)).toBe('12.3K')
    expect(formatCompact(1234567)).toBe('1.2M')
    expect(formatCompact(999)).toBe('999')
    expect(formatCompact(undefined)).toBe('-')
    i18n.global.locale.value = 'tr-TR'
    expect(formatCompact(1500)).toContain('1,5')
  })

  it('formatMoney shows cents only for small amounts', () => {
    expect(formatMoney(3.456)).toBe('$3.46')
    expect(formatMoney(1234.56)).toBe('$1,235')
    expect(formatMoney(null)).toBe('-')
  })

  it('formatBytes picks the unit and precision', () => {
    expect(formatBytes(0)).toBe('-')
    expect(formatBytes(null)).toBe('-')
    expect(formatBytes(512)).toBe('512 B')
    expect(formatBytes(1536)).toBe('1.5 KB')
    expect(formatBytes(10240)).toBe('10 KB')
    expect(formatBytes(5 * 1024 ** 3)).toBe('5.0 GB')
    expect(formatBytes(2 ** 40)).toBe('1024 GB')
  })

  it('formatPercent and formatScore', () => {
    expect(formatPercent(0.8567)).toBe('86%')
    expect(formatPercent(0.8567, 1)).toBe('85.7%')
    expect(formatPercent(null)).toBe('-')
    expect(formatScore(87.6)).toBe('88')
    expect(formatScore(undefined)).toBe('-')
  })
})
