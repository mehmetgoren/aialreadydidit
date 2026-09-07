import { BaseService, clean } from './base-service'
import type { OkDto, Paged } from '@/utils/models/common-models'
import type { AgentCheck, AgentDocs, AppRequestDto } from '@/utils/models/requests-models'

/** api/v1/requests — the "wanted" board. */
export class RequestsService extends BaseService {
  constructor() {
    super('requests')
  }

  list(status?: string, q?: string, sort?: string, page = 1, pageSize = 20) {
    return this.get<Paged<AppRequestDto>>('', clean({ status, q, sort, page, pageSize }))
  }

  getOne(id: number) {
    return this.get<AppRequestDto>(`${id}`)
  }

  create(title: string, description: string) {
    return this.post<AppRequestDto>('', { title, description })
  }

  vote(id: number) {
    return this.post<AppRequestDto>(`${id}/vote`)
  }

  fulfil(id: number, appId: number) {
    return this.post<AppRequestDto>(`${id}/fulfil`, { appId })
  }

  close(id: number) {
    return this.post<OkDto>(`${id}/close`)
  }
}

/** api/v1/agent — the agent entry point (also used by the "For agents" page). */
export class AgentService extends BaseService {
  constructor() {
    super('agent')
  }

  docs() {
    return this.get<AgentDocs>('')
  }

  check(q: string, platform?: string, take = 5) {
    return this.get<AgentCheck>('check', clean({ q, platform, take }))
  }
}
