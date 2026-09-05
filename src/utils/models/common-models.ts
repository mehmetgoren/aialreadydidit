/** Envelope returned by every API endpoint. */
export interface ApiEnvelope<T> {
  statusCode: number
  statusMessage: string
  result: T
  errors: ApiError[] | null
}

export interface ApiError {
  statusCode: number
  message: string
  messageGroup: string
  field?: string | null
}

export class ApiRequestError extends Error {
  constructor(
    message: string,
    public readonly statusCode: number,
    public readonly statusMessage: string,
    public readonly errors: ApiError[] = [],
    public readonly httpStatus?: number,
  ) {
    super(message)
    this.name = 'ApiRequestError'
  }
}

/** Standard paged list shape. */
export interface Paged<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface SelectOption<T = string | number> {
  value: T
  label: string
  disabled?: boolean
}

export interface MenuItem {
  label: string
  icon?: string
  route?: string
  children?: MenuItem[]
  badge?: number | string
  hidden?: boolean
}

export interface AdminListQuery {
  q?: string
  page?: number
  pageSize?: number
  sort?: string
  dir?: 'asc' | 'desc'
  status?: string
  from?: string
  to?: string
  [key: string]: unknown
}

export interface OkDto {
  ok: boolean
}
