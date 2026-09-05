import { BaseService } from './base-service'
import type { OkDto } from '@/utils/models/common-models'
import type { AuthResponse, MeDto, SessionDto, SignInRequest, SignUpRequest, UpdateProfileRequest } from '@/utils/models/user-models'

/** api/v1/account — sign-up / sign-in / refresh / profile. */
export class AccountService extends BaseService {
  constructor() {
    super('account')
  }

  signUp(request: SignUpRequest) {
    return this.post<AuthResponse>('signup', request)
  }

  signIn(request: SignInRequest) {
    return this.post<AuthResponse>('signin', request)
  }

  google(idToken: string, locale?: string) {
    return this.post<AuthResponse>('google', { idToken, locale })
  }

  refresh() {
    return this.post<AuthResponse>('refresh')
  }

  signOut() {
    return this.post<OkDto>('signout')
  }

  me() {
    return this.get<MeDto>('me')
  }

  updateProfile(request: UpdateProfileRequest) {
    return this.put<MeDto>('profile', request)
  }

  changePassword(currentPassword: string | null, newPassword: string) {
    return this.post<OkDto>('password', { currentPassword, newPassword })
  }

  verifyEmail(token: string) {
    return this.post<OkDto>('verify-email', { token })
  }

  resendVerification() {
    return this.post<OkDto>('resend-verification')
  }

  forgotPassword(email: string) {
    return this.post<OkDto>('forgot-password', { email })
  }

  resetPassword(token: string, password: string) {
    return this.post<OkDto>('reset-password', { token, password })
  }

  sessions() {
    return this.get<SessionDto[]>('sessions')
  }

  revokeSession(id: number) {
    return this.delete<OkDto>(`sessions/${id}`)
  }

  revokeAllSessions() {
    return this.delete<OkDto>('sessions')
  }
}
