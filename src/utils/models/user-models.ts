/** GET /account/me */
export interface MeDto {
  id: number
  email: string
  emailVerified: boolean
  username: string
  displayName: string
  avatarUrl: string | null
  bio: string | null
  website: string | null
  role: string
  isAdmin: boolean
  trustLevel: number
  locale: string
  hasPassword: boolean
  hasGoogle: boolean
  unreadNotificationCount: number
  appCount: number
  pendingAppCount: number
  favoriteCount: number
  createdAt: string
}

export interface AuthResponse {
  token: string
  tokenExpireDate: string
  user: MeDto
}

/** What is persisted locally for the signed-in member. */
export interface UserLocalDto {
  token: string | null
  tokenExpireDate: string | null
  user: MeDto | null
}

export interface SignUpRequest {
  email: string
  username: string
  displayName?: string
  password: string
  locale?: string
}

export interface SignInRequest {
  login: string
  password: string
  rememberMe: boolean
}

export interface UpdateProfileRequest {
  displayName: string
  bio?: string | null
  website?: string | null
  locale?: string
}

export interface SessionDto {
  id: number
  createdAt: string
  lastUsedAt: string | null
  expiresAt: string
  ip: string | null
  userAgent: string | null
  isCurrent: boolean
}
