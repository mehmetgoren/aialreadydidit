import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { ApiRequestError, type ApiEnvelope } from '@/utils/models/common-models'
import { localService } from '@/utils/services/local-service'

/**
 * Single axios instance for the API. Base URL and access token are resolved per request from LocalService.
 * The refresh token lives in an httpOnly cookie: on a 401 the interceptor calls /account/refresh once and
 * retries the original request; if that fails the member is signed out.
 */
export const api = axios.create({ timeout: 120_000, withCredentials: true })

api.interceptors.request.use((config) => {
  config.baseURL = localService.getServerAddress()
  config.headers['X-Aadi-Client'] = 'web'
  const user = localService.getCurrentUser()
  if (user?.token) config.headers.Authorization = `Bearer ${user.token}`
  return config
})

type UnauthorizedHandler = () => void
type RefreshHandler = () => Promise<boolean>
let onUnauthorized: UnauthorizedHandler | null = null
let onRefresh: RefreshHandler | null = null
let refreshing: Promise<boolean> | null = null

/** Registered by the router boot so a 401 sends the member back to the login page. */
export function setUnauthorizedHandler(handler: UnauthorizedHandler) {
  onUnauthorized = handler
}
/** Registered by the user store: refreshes the access token through the cookie. */
export function setRefreshHandler(handler: RefreshHandler) {
  onRefresh = handler
}

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiEnvelope<unknown>>) => {
    const config = error.config as (InternalAxiosRequestConfig & { _retried?: boolean }) | undefined
    const url = config?.url ?? ''
    const isAuthCall = url.includes('account/refresh') || url.includes('account/signin') || url.includes('account/signout')
    if (error.response?.status === 401 && config && !config._retried && !isAuthCall && onRefresh) {
      config._retried = true
      refreshing ??= onRefresh().finally(() => (refreshing = null))
      const ok = await refreshing
      if (ok) return api.request(config)
      localService.setCurrentUser(null)
      onUnauthorized?.()
    } else if (error.response?.status === 401 && !isAuthCall) {
      localService.setCurrentUser(null)
      onUnauthorized?.()
    }
    return Promise.reject(normalize(error))
  },
)

function normalize(error: AxiosError<ApiEnvelope<unknown>>): Error {
  const status = error.response?.status
  const body = error.response?.data
  if (body && typeof body === 'object' && 'statusCode' in body) {
    return new ApiRequestError(
      body.errors?.[0]?.message ?? body.statusMessage,
      body.statusCode,
      body.statusMessage,
      body.errors ?? [],
      status,
    )
  }
  if (error.code === 'ERR_NETWORK') {
    return new ApiRequestError('The server cannot be reached. Check your connection.', -1, 'NETWORK', [], status)
  }
  return new ApiRequestError(error.message, status ?? -1, 'HTTP', [], status)
}
