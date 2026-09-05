import { ElMessage, ElMessageBox, ElLoading } from 'element-plus'
import type { Ref } from 'vue'
import { ApiRequestError } from '@/utils/models/common-models'
import { i18n } from '@/boot/i18n'

const t = (key: string, params?: Record<string, unknown>) => i18n.global.t(key, params ?? {})

// ---------------------------------------------------------------- notifications (prototype: notifyE/S/W/I)
export function notifyE(message: string) {
  ElMessage({ type: 'error', message, showClose: true, duration: 5000 })
}
export function notifyS(message: string) {
  ElMessage({ type: 'success', message, duration: 3000 })
}
export function notifyW(message: string) {
  ElMessage({ type: 'warning', message, showClose: true, duration: 4000 })
}
export function notifyI(message: string) {
  ElMessage({ type: 'info', message, duration: 3000 })
}

/** Show the API error message (or a fallback) as a toast and return it. */
export function notifyError(err: unknown, fallback?: string): string {
  const message = errorMessage(err, fallback)
  notifyE(message)
  return message
}

export function errorMessage(err: unknown, fallback?: string): string {
  const fb = fallback ?? t('error_generic')
  if (err instanceof ApiRequestError) return err.errors[0]?.message ?? err.message ?? fb
  if (err instanceof Error && err.message) return err.message
  return fb
}

/** Field → message map from a 422 response (for inline form errors). */
export function fieldErrors(err: unknown): Record<string, string> {
  const out: Record<string, string> = {}
  if (err instanceof ApiRequestError) for (const e of err.errors) if (e.field && !out[e.field]) out[e.field] = e.message
  return out
}

// ---------------------------------------------------------------- dialogs
export async function confirmX(message: string, title?: string, confirmText?: string, cancelText?: string): Promise<boolean> {
  try {
    await ElMessageBox.confirm(message, title ?? t('confirm'), {
      type: 'warning',
      confirmButtonText: confirmText ?? t('yes'),
      cancelButtonText: cancelText ?? t('cancel'),
      closeOnClickModal: false,
    })
    return true
  } catch {
    return false
  }
}

export async function promptX(message: string, title?: string, defaultValue = ''): Promise<string | null> {
  try {
    const { value } = await ElMessageBox.prompt(message, title ?? t('input'), {
      inputValue: defaultValue,
      confirmButtonText: t('ok'),
      cancelButtonText: t('cancel'),
    })
    return value
  } catch {
    return null
  }
}

// ---------------------------------------------------------------- async helpers
export async function loading<T>(fn: () => Promise<T>, text?: string): Promise<T> {
  const instance = ElLoading.service({ lock: true, text: text ?? t('loading'), background: 'rgba(255,255,255,0.6)' })
  try {
    return await fn()
  } finally {
    instance.close()
  }
}

/** Set a boolean ref while fn runs (button loading / disabled states). */
export async function enableAfter<T>(flag: Ref<boolean>, fn: () => Promise<T>): Promise<T> {
  flag.value = true
  try {
    return await fn()
  } finally {
    flag.value = false
  }
}

export function sleep(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms))
}

export function debounce<A extends unknown[]>(fn: (...args: A) => void, ms: number) {
  let timer: ReturnType<typeof setTimeout> | null = null
  return (...args: A) => {
    if (timer) clearTimeout(timer)
    timer = setTimeout(() => fn(...args), ms)
  }
}

// ---------------------------------------------------------------- misc
export function isNullOrEmpty(v: string | null | undefined): boolean {
  return v === null || v === undefined || v.trim().length === 0
}

export async function copyText(text: string): Promise<boolean> {
  try {
    await navigator.clipboard.writeText(text)
    notifyS(t('copied'))
    return true
  } catch {
    notifyE(t('copy_failed'))
    return false
  }
}

export function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = fileName
  document.body.appendChild(a)
  a.click()
  a.remove()
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}

/** Resolve /files/... and /api/... URLs against the API origin when the API is not behind the same origin. */
export function assetUrl(path: string | null | undefined): string {
  if (!path) return ''
  if (path.startsWith('http')) return path
  const base = import.meta.env.VITE_API_BASE_URL || '/api/v1'
  if (base.startsWith('http')) return base.replace(/\/api\/v1\/?$/, '') + path
  return path
}

export function getFlagImgSrc(locale: string): string {
  const tr =
    '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1200 800"><rect width="1200" height="800" fill="#E30A17"/><circle cx="425" cy="400" r="200" fill="#fff"/><circle cx="475" cy="400" r="160" fill="#E30A17"/><polygon fill="#fff" points="583.3,400 764.3,458.8 652.4,304.8 652.4,495.2 764.3,341.2"/></svg>'
  const en =
    '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 60 30"><clipPath id="s"><path d="M0,0 v30 h60 v-30 z"/></clipPath><path d="M0,0 v30 h60 v-30 z" fill="#012169"/><path d="M0,0 L60,30 M60,0 L0,30" stroke="#fff" stroke-width="6"/><path d="M0,0 L60,30 M60,0 L0,30" clip-path="url(#s)" stroke="#C8102E" stroke-width="4"/><path d="M30,0 v30 M0,15 h60" stroke="#fff" stroke-width="10"/><path d="M30,0 v30 M0,15 h60" stroke="#C8102E" stroke-width="6"/></svg>'
  return 'data:image/svg+xml;base64,' + btoa(locale.startsWith('tr') ? tr : en)
}

export const PLATFORM_ICONS: Record<string, string> = {
  windows: '🪟',
  linux: '🐧',
  macos: '🍎',
  web: '🌐',
  android: '🤖',
  ios: '📱',
  docker: '🐳',
  cli: '⌨️',
}

export function platformIcon(code: string | null | undefined): string {
  return (code && PLATFORM_ICONS[code]) || '📦'
}
