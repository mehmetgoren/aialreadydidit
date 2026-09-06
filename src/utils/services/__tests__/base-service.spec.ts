import { describe, expect, it, vi } from 'vitest'
import type { AxiosResponse } from 'axios'
import { api } from '@/boot/axios'
import { BaseService, clean } from '@/utils/services/base-service'
import { ApiRequestError, type ApiEnvelope } from '@/utils/models/common-models'

const envelope = <T>(result: T, statusCode = 200, errors: ApiEnvelope<T>['errors'] = null): AxiosResponse<ApiEnvelope<T>> =>
  ({ data: { statusCode, statusMessage: statusCode === 200 ? 'OK' : 'ERROR', result, errors }, status: 200 }) as AxiosResponse<ApiEnvelope<T>>

class DemoService extends BaseService {
  constructor() {
    super('demo')
  }
  list(params?: Record<string, unknown>) {
    return this.get<string[]>('items', params)
  }
  create(body: unknown) {
    return this.post<{ id: number }>('/items', body)
  }
  root() {
    return this.get<string>('')
  }
  rename(id: number, name: string) {
    return this.put<string>(`items/${id}`, { name })
  }
  tweak(id: number) {
    return this.patch<string>(`items/${id}`)
  }
  remove(id: number) {
    return this.delete<boolean>(`items/${id}`, { force: true })
  }
  send(form: FormData, onProgress?: (p: number) => void) {
    return this.upload<string>('files', form, onProgress)
  }
}

describe('clean', () => {
  it('drops undefined, null, empty string and false but keeps 0 and true', () => {
    expect(clean({ q: '', page: 0, featured: false, hidden: true, x: undefined, y: null, tags: 'a,b' })).toEqual({ page: 0, hidden: true, tags: 'a,b' })
  })
})

describe('BaseService.unwrap', () => {
  it('returns result on 200 and 207', () => {
    expect(BaseService.unwrap(envelope([1, 2]))).toEqual([1, 2])
    expect(BaseService.unwrap(envelope('partial', 207, [{ statusCode: 207, message: 'some failed', messageGroup: 'warn' }]))).toBe('partial')
  })

  it('throws ApiRequestError with the first error message on failure', () => {
    const res = envelope(null, 422, [
      { statusCode: 422, message: 'Name is required', messageGroup: 'validation', field: 'name' },
      { statusCode: 422, message: 'Also bad', messageGroup: 'validation', field: 'slug' },
    ])
    let caught: unknown
    try {
      BaseService.unwrap(res)
    } catch (e) {
      caught = e
    }
    expect(caught).toBeInstanceOf(ApiRequestError)
    const err = caught as ApiRequestError
    expect(err.message).toBe('Name is required')
    expect(err.statusCode).toBe(422)
    expect(err.errors).toHaveLength(2)
    expect(err.httpStatus).toBe(200)
  })

  it('treats errors inside a 200 envelope as failure and falls back to statusMessage', () => {
    expect(() => BaseService.unwrap(envelope(null, 200, [{ statusCode: 500, message: 'oops', messageGroup: 'x' }]))).toThrow('oops')
    expect(() => BaseService.unwrap(envelope(null, 500))).toThrow('ERROR')
  })

  it('passes non-envelope bodies through untouched', () => {
    expect(BaseService.unwrap({ data: 'raw text', status: 200 } as AxiosResponse)).toBe('raw text')
    expect(BaseService.unwrap({ data: { foo: 1 }, status: 200 } as AxiosResponse)).toEqual({ foo: 1 })
  })
})

describe('BaseService request helpers', () => {
  it('build controller-relative URLs and forward params / bodies', async () => {
    const spy = vi.spyOn(api, 'request').mockImplementation(async (config) => envelope(config))
    const svc = new DemoService()

    expect((await svc.list({ q: 'x' })) as unknown).toMatchObject({ method: 'GET', url: 'demo/items', params: { q: 'x' } })
    expect((await svc.create({ a: 1 })) as unknown).toMatchObject({ method: 'POST', url: 'demo/items', data: { a: 1 } })
    expect((await svc.root()) as unknown).toMatchObject({ method: 'GET', url: 'demo' })
    expect((await svc.rename(3, 'n')) as unknown).toMatchObject({ method: 'PUT', url: 'demo/items/3', data: { name: 'n' } })
    expect((await svc.tweak(4)) as unknown).toMatchObject({ method: 'PATCH', url: 'demo/items/4' })
    expect((await svc.remove(5)) as unknown).toMatchObject({ method: 'DELETE', url: 'demo/items/5', params: { force: true } })
    expect(spy).toHaveBeenCalledTimes(6)
  })

  it('upload sends multipart without timeout and reports progress', async () => {
    vi.spyOn(api, 'request').mockImplementation(async (config) => {
      config.onUploadProgress?.({ loaded: 50, total: 200, bytes: 50, lengthComputable: true })
      config.onUploadProgress?.({ loaded: 10, total: undefined, bytes: 10, lengthComputable: false })
      return envelope(config)
    })
    const progress = vi.fn()
    const form = new FormData()
    const cfg = (await new DemoService().send(form, progress)) as unknown as Record<string, unknown>
    expect(cfg).toMatchObject({ method: 'POST', url: 'demo/files', timeout: 0, headers: { 'Content-Type': 'multipart/form-data' } })
    expect(cfg.data).toBe(form)
    expect(progress).toHaveBeenCalledTimes(1)
    expect(progress).toHaveBeenCalledWith(25)
  })

  it('surfaces API failures from request helpers as ApiRequestError', async () => {
    vi.spyOn(api, 'request').mockResolvedValue(envelope(null, 404))
    await expect(new DemoService().root()).rejects.toBeInstanceOf(ApiRequestError)
  })
})
