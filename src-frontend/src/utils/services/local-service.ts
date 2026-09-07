import type { UserLocalDto } from '@/utils/models/user-models'

export enum Themes {
  Light = 0,
  Dark = 1,
}

const KEYS = {
  serverAddress: 'aadi_server_address',
  user: 'aadi_user',
  theme: 'aadi_theme',
  language: 'aadi_language',
  searchMode: 'aadi_search_mode',
} as const

/**
 * Thin wrapper over localStorage (mirrors the prototype's LocalService).
 * Every read is guarded so private mode / blocked storage never throws.
 */
export class LocalService {
  private get(key: string): string | null {
    try {
      return localStorage.getItem(key)
    } catch {
      return null
    }
  }

  private set(key: string, value: string | null) {
    try {
      if (value === null) localStorage.removeItem(key)
      else localStorage.setItem(key, value)
    } catch {
      /* ignore */
    }
  }

  /** Absolute API root, e.g. http://localhost:5190/api/v1 — configurable at runtime. */
  getServerAddress(): string {
    return this.get(KEYS.serverAddress) || import.meta.env.VITE_API_BASE_URL || '/api/v1'
  }

  setServerAddress(address: string | null) {
    this.set(KEYS.serverAddress, address)
  }

  /** Origin of the API (for /files/... image URLs when the API is on another origin). */
  getApiOrigin(): string {
    const base = this.getServerAddress()
    if (base.startsWith('http')) return base.replace(/\/api\/v1\/?$/, '')
    return ''
  }

  getCurrentUser(): UserLocalDto | null {
    const raw = this.get(KEYS.user)
    if (!raw) return null
    try {
      return JSON.parse(raw) as UserLocalDto
    } catch {
      return null
    }
  }

  setCurrentUser(user: UserLocalDto | null) {
    this.set(KEYS.user, user ? JSON.stringify(user) : null)
  }

  getTheme(): Themes {
    return this.get(KEYS.theme) === String(Themes.Dark) ? Themes.Dark : Themes.Light
  }

  setTheme(theme: Themes) {
    this.set(KEYS.theme, String(theme))
  }

  getLanguage(): string | null {
    return this.get(KEYS.language)
  }

  setLanguage(lang: string) {
    this.set(KEYS.language, lang)
  }

  getSearchMode(): string | null {
    return this.get(KEYS.searchMode)
  }

  setSearchMode(mode: string) {
    this.set(KEYS.searchMode, mode)
  }
}

export const localService = new LocalService()
