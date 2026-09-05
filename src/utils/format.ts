import dayjs from 'dayjs'
import 'dayjs/locale/tr'
import relativeTime from 'dayjs/plugin/relativeTime'
import { i18n } from '@/boot/i18n'

dayjs.extend(relativeTime)

function loc() {
  return String(i18n.global.locale.value) === 'tr-TR' ? 'tr' : 'en'
}

export function formatDate(value: string | Date | null | undefined, pattern = 'DD MMM YYYY'): string {
  if (!value) return '-'
  return dayjs(value).locale(loc()).format(pattern)
}

export function formatDateTime(value: string | Date | null | undefined): string {
  return formatDate(value, 'DD MMM YYYY HH:mm')
}

export function fromNow(value: string | Date | null | undefined): string {
  if (!value) return ''
  return dayjs(value).locale(loc()).fromNow()
}

export function toApiDate(value: Date | string | null | undefined): string | undefined {
  if (!value) return undefined
  return dayjs(value).format('YYYY-MM-DD')
}

export function formatNumber(value: number | null | undefined, digits = 0): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '-'
  return value.toLocaleString(loc() === 'tr' ? 'tr-TR' : 'en-US', { maximumFractionDigits: digits, minimumFractionDigits: digits })
}

/** 1234567 → "1.2M", 12345 → "12.3K" */
export function formatCompact(value: number | null | undefined): string {
  if (value === null || value === undefined) return '-'
  return new Intl.NumberFormat(loc() === 'tr' ? 'tr-TR' : 'en-US', { notation: 'compact', maximumFractionDigits: 1 }).format(value)
}

export function formatMoney(value: number | null | undefined, currency = 'USD'): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '-'
  return new Intl.NumberFormat(loc() === 'tr' ? 'tr-TR' : 'en-US', { style: 'currency', currency, maximumFractionDigits: value < 10 ? 2 : 0 }).format(value)
}

export function formatBytes(bytes: number | null | undefined): string {
  if (!bytes) return '-'
  const units = ['B', 'KB', 'MB', 'GB']
  let size = bytes
  let unit = 0
  while (size >= 1024 && unit < units.length - 1) {
    size /= 1024
    unit++
  }
  return `${size.toFixed(size < 10 && unit > 0 ? 1 : 0)} ${units[unit]}`
}

export function formatPercent(value: number | null | undefined, digits = 0): string {
  if (value === null || value === undefined) return '-'
  return `${(value * 100).toFixed(digits)}%`
}

export function formatScore(value: number | null | undefined): string {
  if (value === null || value === undefined) return '-'
  return Math.round(value).toString()
}

export { dayjs }
