import { beforeEach, describe, expect, it, vi } from 'vitest'
import { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios'
import { api, setRefreshHandler, setUnauthorizedHandler } from '@/boot/axios'
import { localService } from '@/utils/services/local-service'
import { ApiRequestError } from '@/utils/models/common-models'
import type { UserLocalDto } from '@/utils/models/user-models'

type Handler = (config: InternalAxiosRequestConfig) => Promise<AxiosResponse>

const ok = (config: InternalAxiosRequestConfig, data: unknown = { statusCode: 200, statusMessage: 'OK', result: 'fine', errors: null }): AxiosResponse =>
  ({ data, status: 200, statusText: 'OK', headers: {}, config }) as AxiosResponse

function fail(config: InternalAxiosRequestConfig, status: number, data?: unknown): never {
  const response = { data, status, statusText: 'ERR', headers: {}, config } as AxiosResponse
  throw new AxiosError(`Request failed with status code ${status}`, 'ERR_BAD_REQUEST', config, null, response)
}

const asUser = (token: string): UserLocalDto => ({ token, tokenExpireDate: null, user: null })

/** Awaits a request that must fail and returns the normalised error. */
const caught = (p: Promise<unknown>): Promise<ApiRequestError> =>
  p.then(
    () => {
      throw new Error('expected the request to fail')
    },
    (e: unknown) => e as ApiRequestError,
  )

/** Installs a fake transport so the interceptors run against scripted responses. */
function transport(handler: Handler) {
  const calls: InternalAxiosRequestConfig[] = []
  api.defaults.adapter = (config) => {
    calls.push(config)
    return handler(config)
  }
  return calls
}

describe('axios boot', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.stubEnv('VITE_API_BASE_URL', 'http://api.test/api/v1')
    setRefreshHandler(async () => false)
    setUnauthorizedHandler(() => {})
  })

  it('adds base URL, client header and bearer token per request', async () => {
    localService.setCurrentUser(asUser('abc'))
    const calls = transport(async (c) => ok(c))
    await api.get('catalog/home')
    expect(calls[0]!.baseURL).toBe('http://api.test/api/v1')
    expect(calls[0]!.headers['X-Aadi-Client']).toBe('web')
    expect(calls[0]!.headers.Authorization).toBe('Bearer abc')

    localService.setCurrentUser(null)
    await api.get('catalog/home')
    expect(calls[1]!.headers.Authorization).toBeUndefined()
  })

  it('refreshes once on 401 and retries the original request', async () => {
    localService.setCurrentUser(asUser('old'))
    const refresh = vi.fn(async () => {
      localService.setCurrentUser(asUser('new'))
      return true
    })
    setRefreshHandler(refresh)
    const calls = transport(async (c) => (c.headers.Authorization === 'Bearer old' ? fail(c, 401) : ok(c)))

    const [a, b] = await Promise.all([api.get('my/overview'), api.get('my/notifications')])
    expect(a.data.result).toBe('fine')
    expect(b.data.result).toBe('fine')
    expect(refresh).toHaveBeenCalledTimes(1)
    expect(calls).toHaveLength(4)
    expect(calls.slice(2).every((c) => c.headers.Authorization === 'Bearer new')).toBe(true)
  })

  it('signs the member out when the refresh fails', async () => {
    localService.setCurrentUser(asUser('old'))
    const unauthorized = vi.fn()
    setUnauthorizedHandler(unauthorized)
    setRefreshHandler(async () => false)
    transport(async (c) => fail(c, 401, { statusCode: 401, statusMessage: 'Unauthorized', result: null, errors: null }))

    await expect(api.get('my/overview')).rejects.toMatchObject({ name: 'ApiRequestError', statusCode: 401, httpStatus: 401 })
    expect(unauthorized).toHaveBeenCalledTimes(1)
    expect(localService.getCurrentUser()).toBeNull()
  })

  it('does not retry a failed retry (no infinite loop)', async () => {
    localService.setCurrentUser(asUser('old'))
    const refresh = vi.fn(async () => true)
    setRefreshHandler(refresh)
    const calls = transport(async (c) => fail(c, 401))
    await expect(api.get('my/overview')).rejects.toBeInstanceOf(ApiRequestError)
    expect(refresh).toHaveBeenCalledTimes(1)
    expect(calls).toHaveLength(2)
  })

  it('never refreshes for sign-in / refresh / sign-out calls', async () => {
    const refresh = vi.fn(async () => true)
    const unauthorized = vi.fn()
    setRefreshHandler(refresh)
    setUnauthorizedHandler(unauthorized)
    transport(async (c) => fail(c, 401, { statusCode: 401, statusMessage: 'Bad credentials', result: null, errors: [{ statusCode: 401, message: 'Wrong password', messageGroup: 'auth' }] }))

    await expect(api.post('account/signin', {})).rejects.toThrow('Wrong password')
    await expect(api.post('account/refresh')).rejects.toBeInstanceOf(ApiRequestError)
    expect(refresh).not.toHaveBeenCalled()
    expect(unauthorized).not.toHaveBeenCalled()
  })

  it('normalises envelope errors, network errors and plain HTTP errors', async () => {
    transport(async (c) => fail(c, 422, { statusCode: 422, statusMessage: 'Validation', result: null, errors: [{ statusCode: 422, message: 'Name is required', messageGroup: 'v', field: 'name' }] }))
    const e1 = await caught(api.get('x'))
    expect(e1).toBeInstanceOf(ApiRequestError)
    expect(e1.message).toBe('Name is required')
    expect(e1.errors[0]?.field).toBe('name')
    expect(e1.httpStatus).toBe(422)

    transport(async (c) => {
      throw new AxiosError('Network Error', 'ERR_NETWORK', c)
    })
    const e2 = await caught(api.get('x'))
    expect(e2.statusCode).toBe(-1)
    expect(e2.statusMessage).toBe('NETWORK')
    expect(e2.message).toContain('cannot be reached')

    transport(async (c) => fail(c, 502, '<html>bad gateway</html>'))
    const e3 = await caught(api.get('x'))
    expect(e3.statusMessage).toBe('HTTP')
    expect(e3.statusCode).toBe(502)
  })
})
