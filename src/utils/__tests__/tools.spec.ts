import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ref } from 'vue'
import { ElLoading, ElMessage, ElMessageBox } from 'element-plus'
import { ApiRequestError } from '@/utils/models/common-models'
import {
  assetUrl,
  confirmX,
  copyText,
  debounce,
  enableAfter,
  errorMessage,
  fieldErrors,
  getFlagImgSrc,
  isNullOrEmpty,
  loading,
  notifyError,
  platformIcon,
  promptX,
} from '@/utils/tools'

vi.mock('element-plus', async (original) => {
  const mod = await original<typeof import('element-plus')>()
  return {
    ...mod,
    ElMessage: vi.fn(),
    ElMessageBox: { confirm: vi.fn(), prompt: vi.fn() },
    ElLoading: { service: vi.fn() },
  }
})

const apiError = (errors: { message: string; field?: string | null }[], message = 'Validation failed') =>
  new ApiRequestError(message, 422, 'VALIDATION', errors.map((e) => ({ statusCode: 422, messageGroup: 'validation', ...e })), 422)

describe('tools: error helpers', () => {
  it('errorMessage prefers the first API error, then Error.message, then the fallback', () => {
    expect(errorMessage(apiError([{ message: 'Name is required', field: 'name' }]))).toBe('Name is required')
    expect(errorMessage(new ApiRequestError('Boom', 500, 'ERROR'))).toBe('Boom')
    expect(errorMessage(new Error('plain'))).toBe('plain')
    expect(errorMessage('weird', 'fallback')).toBe('fallback')
    expect(errorMessage(null)).toBe('Something went wrong. Please try again.')
  })

  it('fieldErrors keeps the first message per field and ignores non-API errors', () => {
    const err = apiError([
      { message: 'Name is required', field: 'name' },
      { message: 'Name too short', field: 'name' },
      { message: 'General', field: null },
      { message: 'Bad e-mail', field: 'email' },
    ])
    expect(fieldErrors(err)).toEqual({ name: 'Name is required', email: 'Bad e-mail' })
    expect(fieldErrors(new Error('x'))).toEqual({})
  })

  it('notifyError toasts and returns the message', () => {
    const message = notifyError(new Error('failed'))
    expect(message).toBe('failed')
    expect(ElMessage).toHaveBeenCalledWith(expect.objectContaining({ type: 'error', message: 'failed' }))
  })
})

describe('tools: dialogs and async helpers', () => {
  beforeEach(() => {
    vi.mocked(ElMessageBox.confirm).mockReset()
    vi.mocked(ElMessageBox.prompt).mockReset()
  })

  it('confirmX resolves true on confirm and false on cancel', async () => {
    vi.mocked(ElMessageBox.confirm).mockResolvedValueOnce('confirm' as never)
    expect(await confirmX('Delete?')).toBe(true)
    expect(ElMessageBox.confirm).toHaveBeenCalledWith('Delete?', 'Confirm', expect.objectContaining({ confirmButtonText: 'Yes', cancelButtonText: 'Cancel' }))
    vi.mocked(ElMessageBox.confirm).mockRejectedValueOnce('cancel')
    expect(await confirmX('Delete?', 'Title', 'Go', 'Stop')).toBe(false)
    expect(ElMessageBox.confirm).toHaveBeenLastCalledWith('Delete?', 'Title', expect.objectContaining({ confirmButtonText: 'Go', cancelButtonText: 'Stop' }))
  })

  it('promptX returns the typed value or null', async () => {
    vi.mocked(ElMessageBox.prompt).mockResolvedValueOnce({ value: 'typed', action: 'confirm' } as never)
    expect(await promptX('Name?', undefined, 'seed')).toBe('typed')
    expect(ElMessageBox.prompt).toHaveBeenCalledWith('Name?', 'Input', expect.objectContaining({ inputValue: 'seed' }))
    vi.mocked(ElMessageBox.prompt).mockRejectedValueOnce('cancel')
    expect(await promptX('Name?')).toBeNull()
  })

  it('loading opens a full-screen loader and always closes it', async () => {
    const close = vi.fn()
    vi.mocked(ElLoading.service).mockReturnValue({ close } as never)
    expect(await loading(async () => 42)).toBe(42)
    expect(close).toHaveBeenCalledTimes(1)
    await expect(loading(async () => Promise.reject(new Error('x')))).rejects.toThrow('x')
    expect(close).toHaveBeenCalledTimes(2)
  })

  it('enableAfter toggles the flag around the call, even when it throws', async () => {
    const flag = ref(false)
    let seen = false
    await enableAfter(flag, async () => {
      seen = flag.value
    })
    expect(seen).toBe(true)
    expect(flag.value).toBe(false)
    await expect(enableAfter(flag, async () => Promise.reject(new Error('x')))).rejects.toThrow()
    expect(flag.value).toBe(false)
  })

  it('debounce collapses rapid calls into the last one', () => {
    vi.useFakeTimers()
    const fn = vi.fn()
    const d = debounce(fn, 100)
    d(1)
    d(2)
    d(3)
    vi.advanceTimersByTime(99)
    expect(fn).not.toHaveBeenCalled()
    vi.advanceTimersByTime(1)
    expect(fn).toHaveBeenCalledTimes(1)
    expect(fn).toHaveBeenCalledWith(3)
    vi.useRealTimers()
  })
})

describe('tools: misc', () => {
  it('isNullOrEmpty', () => {
    expect(isNullOrEmpty(null)).toBe(true)
    expect(isNullOrEmpty(undefined)).toBe(true)
    expect(isNullOrEmpty('   ')).toBe(true)
    expect(isNullOrEmpty(' a ')).toBe(false)
  })

  it('copyText reports success and failure', async () => {
    const writeText = vi.fn().mockResolvedValueOnce(undefined).mockRejectedValueOnce(new Error('denied'))
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true })
    expect(await copyText('hello')).toBe(true)
    expect(writeText).toHaveBeenCalledWith('hello')
    expect(ElMessage).toHaveBeenLastCalledWith(expect.objectContaining({ type: 'success', message: 'Copied to clipboard.' }))
    expect(await copyText('hello')).toBe(false)
    expect(ElMessage).toHaveBeenLastCalledWith(expect.objectContaining({ type: 'error' }))
  })

  it('assetUrl resolves relative paths only when the API lives on another origin', () => {
    expect(assetUrl(null)).toBe('')
    expect(assetUrl('https://cdn/x.png')).toBe('https://cdn/x.png')
    vi.stubEnv('VITE_API_BASE_URL', '')
    expect(assetUrl('/files/shots/a.png')).toBe('/files/shots/a.png')
    vi.stubEnv('VITE_API_BASE_URL', 'http://api.local:5190/api/v1/')
    expect(assetUrl('/files/shots/a.png')).toBe('http://api.local:5190/files/shots/a.png')
    vi.stubEnv('VITE_API_BASE_URL', 'http://api.local:5190/api/v1')
    expect(assetUrl('/api/v1/apps/x/download/1')).toBe('http://api.local:5190/api/v1/apps/x/download/1')
  })

  it('platformIcon falls back to a box', () => {
    expect(platformIcon('linux')).toBe('🐧')
    expect(platformIcon('docker')).toBe('🐳')
    expect(platformIcon('amiga')).toBe('📦')
    expect(platformIcon(null)).toBe('📦')
  })

  it('getFlagImgSrc returns an inline SVG per locale', () => {
    const tr = getFlagImgSrc('tr-TR')
    const en = getFlagImgSrc('en-US')
    expect(tr.startsWith('data:image/svg+xml;base64,')).toBe(true)
    expect(atob(tr.split(',')[1]!)).toContain('#E30A17')
    expect(atob(en.split(',')[1]!)).toContain('#012169')
  })
})
