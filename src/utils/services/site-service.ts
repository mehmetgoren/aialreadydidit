import { BaseService } from './base-service'
import type { SiteConfig } from '@/utils/models/site-models'
import type { SavingsDto } from '@/utils/models/catalog-models'

/** api/v1/site — boot config + savings counter. */
export class SiteService extends BaseService {
  constructor() {
    super('site')
  }

  config() {
    return this.get<SiteConfig>('config')
  }

  savings() {
    return this.get<SavingsDto>('savings')
  }
}
