import { beforeEach, describe, expect, it, vi } from 'vitest'
import { LocalService, Themes } from '@/utils/services/local-service'
import type { UserLocalDto } from '@/utils/models/user-models'

describe('LocalService', () => {
  let svc: LocalService
  beforeEach(() => {
    localStorage.clear()
    svc = new LocalService()
  })

  it('server address falls back to the env value, then /api/v1', () => {
    vi.stubEnv('VITE_API_BASE_URL', '')
    expect(svc.getServerAddress()).toBe('/api/v1')
    expect(svc.getApiOrigin()).toBe('')
    vi.stubEnv('VITE_API_BASE_URL', 'http://api.local/api/v1')
    expect(svc.getServerAddress()).toBe('http://api.local/api/v1')
    expect(svc.getApiOrigin()).toBe('http://api.local')
    svc.setServerAddress('https://store.example/api/v1/')
    expect(svc.getServerAddress()).toBe('https://store.example/api/v1/')
    expect(svc.getApiOrigin()).toBe('https://store.example')
    svc.setServerAddress(null)
    expect(svc.getServerAddress()).toBe('http://api.local/api/v1')
  })

  it('round-trips the current user and survives corrupt JSON', () => {
    expect(svc.getCurrentUser()).toBeNull()
    const user = { token: 't', tokenExpireDate: '2026-09-06', user: null } as UserLocalDto
    svc.setCurrentUser(user)
    expect(svc.getCurrentUser()).toEqual(user)
    expect(localStorage.getItem('aadi_user')).toContain('"token":"t"')
    localStorage.setItem('aadi_user', '{not json')
    expect(svc.getCurrentUser()).toBeNull()
    svc.setCurrentUser(null)
    expect(localStorage.getItem('aadi_user')).toBeNull()
  })

  it('theme defaults to light and persists dark', () => {
    expect(svc.getTheme()).toBe(Themes.Light)
    svc.setTheme(Themes.Dark)
    expect(svc.getTheme()).toBe(Themes.Dark)
    localStorage.setItem('aadi_theme', 'garbage')
    expect(svc.getTheme()).toBe(Themes.Light)
  })

  it('language and search mode', () => {
    expect(svc.getLanguage()).toBeNull()
    svc.setLanguage('tr-TR')
    expect(svc.getLanguage()).toBe('tr-TR')
    svc.setSearchMode('semantic')
    expect(svc.getSearchMode()).toBe('semantic')
  })

  it('never throws when storage is unavailable', () => {
    const getItem = vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('blocked')
    })
    const setItem = vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('blocked')
    })
    expect(() => svc.setLanguage('en-US')).not.toThrow()
    expect(svc.getLanguage()).toBeNull()
    expect(svc.getTheme()).toBe(Themes.Light)
    getItem.mockRestore()
    setItem.mockRestore()
  })
})
