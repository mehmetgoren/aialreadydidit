import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import type { AuthResponse, MeDto, UserLocalDto } from '@/utils/models/user-models'
import { localService } from '@/utils/services/local-service'
import { AccountService } from '@/utils/services/account-service'
import { setRefreshHandler } from '@/boot/axios'

export const useUserStore = defineStore('user', () => {
  const _user = ref<UserLocalDto | null>(localService.getCurrentUser())

  const currentUser = computed(() => _user.value)
  const me = computed<MeDto | null>(() => _user.value?.user ?? null)
  const isAuthenticated = computed(() => Boolean(_user.value?.token))
  const isAdmin = computed(() => _user.value?.user?.isAdmin === true)
  const displayName = computed(() => me.value?.displayName || me.value?.username || '')

  function setCurrentUser(user: UserLocalDto | null) {
    _user.value = user
    localService.setCurrentUser(user)
  }

  function applyAuth(auth: AuthResponse) {
    setCurrentUser({ token: auth.token, tokenExpireDate: auth.tokenExpireDate, user: auth.user })
  }

  /** Re-fetch /account/me (counters for the header). */
  async function refreshMe() {
    if (!_user.value?.token) return null
    const fresh = await new AccountService().me()
    setCurrentUser({ ..._user.value, user: fresh })
    return fresh
  }

  function patchMe(patch: Partial<MeDto>) {
    if (!_user.value?.user) return
    setCurrentUser({ ..._user.value, user: { ..._user.value.user, ...patch } })
  }

  /** Access token renewal through the refresh cookie (called by the axios interceptor on 401). */
  async function refreshToken(): Promise<boolean> {
    try {
      const auth = await new AccountService().refresh()
      applyAuth(auth)
      return true
    } catch {
      return false
    }
  }

  async function signOut() {
    try {
      await new AccountService().signOut()
    } catch {
      /* ignore */
    }
    setCurrentUser(null)
  }

  setRefreshHandler(refreshToken)

  return { currentUser, me, isAuthenticated, isAdmin, displayName, setCurrentUser, applyAuth, refreshMe, patchMe, refreshToken, signOut }
})
