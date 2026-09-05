import type { AxiosRequestConfig, AxiosResponse } from 'axios'
import { api } from '@/boot/axios'
import { ApiRequestError, type ApiEnvelope } from '@/utils/models/common-models'

/** Strips empty / false values so the URL only carries the filters that are actually on. */
export function clean(query: object): Record<string, unknown> {
  const out: Record<string, unknown> = {}
  for (const [key, value] of Object.entries(query)) {
    if (value === undefined || value === null || value === '' || value === false) continue
    out[key] = value
  }
  return out
}

/**
 * Base class for one-service-per-controller classes (prototype pattern).
 * All helpers unwrap the `{ statusCode, statusMessage, result, errors }` envelope and throw
 * `ApiRequestError` when the API reports a failure inside a 2xx response.
 */
export abstract class BaseService {
  protected constructor(protected readonly controller: string) {}

  protected url(path = ''): string {
    const p = path.startsWith('/') ? path.slice(1) : path
    return p ? `${this.controller}/${p}` : this.controller
  }

  protected async request<T>(config: AxiosRequestConfig): Promise<T> {
    const response: AxiosResponse<ApiEnvelope<T>> = await api.request<ApiEnvelope<T>>(config)
    return BaseService.unwrap(response)
  }

  protected get<T>(path: string, params?: Record<string, unknown>, config?: AxiosRequestConfig) {
    return this.request<T>({ method: 'GET', url: this.url(path), params, ...config })
  }

  protected post<T>(path: string, data?: unknown, config?: AxiosRequestConfig) {
    return this.request<T>({ method: 'POST', url: this.url(path), data, ...config })
  }

  protected put<T>(path: string, data?: unknown, config?: AxiosRequestConfig) {
    return this.request<T>({ method: 'PUT', url: this.url(path), data, ...config })
  }

  protected patch<T>(path: string, data?: unknown, config?: AxiosRequestConfig) {
    return this.request<T>({ method: 'PATCH', url: this.url(path), data, ...config })
  }

  protected delete<T>(path: string, params?: Record<string, unknown>, config?: AxiosRequestConfig) {
    return this.request<T>({ method: 'DELETE', url: this.url(path), params, ...config })
  }

  /** multipart/form-data upload with optional progress callback. */
  protected upload<T>(path: string, form: FormData, onProgress?: (percent: number) => void) {
    return this.request<T>({
      method: 'POST',
      url: this.url(path),
      data: form,
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: 0,
      onUploadProgress: (e) => {
        if (onProgress && e.total) onProgress(Math.round((e.loaded * 100) / e.total))
      },
    })
  }

  static unwrap<T>(response: AxiosResponse<ApiEnvelope<T>>): T {
    const body = response.data
    if (body && typeof body === 'object' && 'statusCode' in body) {
      const ok = body.statusCode === 200 || body.statusCode === 207
      if (!ok || (body.errors && body.errors.length > 0 && body.statusCode !== 207)) {
        throw new ApiRequestError(
          body.errors?.[0]?.message ?? body.statusMessage,
          body.statusCode,
          body.statusMessage,
          body.errors ?? [],
          response.status,
        )
      }
      return body.result
    }
    return body as unknown as T
  }
}
