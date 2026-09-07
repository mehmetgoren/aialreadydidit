import { BaseService, clean } from './base-service'
import type {
  AppDetail,
  AppCardDto,
  AppQuery,
  AppSearchResult,
  CategoryDetail,
  CategoryNode,
  CollectionDto,
  HomeContent,
  LicenseDto,
  LineageNode,
  LlmModelDto,
  PlatformDto,
  TagDto,
  UploaderDto,
} from '@/utils/models/catalog-models'

/** GET api/v1/catalog/... — storefront browsing (public). */
export class CatalogService extends BaseService {
  constructor() {
    super('catalog')
  }

  getCategories() {
    return this.get<CategoryNode[]>('categories')
  }

  getCategory(slug: string) {
    return this.get<CategoryDetail>(`categories/${encodeURIComponent(slug)}`)
  }

  getPlatforms() {
    return this.get<PlatformDto[]>('platforms')
  }

  getLicenses() {
    return this.get<LicenseDto[]>('licenses')
  }

  getLlmModels() {
    return this.get<LlmModelDto[]>('llm-models')
  }

  getTags(q?: string, take = 30) {
    return this.get<TagDto[]>('tags', clean({ q, take }))
  }

  getHome() {
    return this.get<HomeContent>('home')
  }

  search(query: AppQuery) {
    return this.get<AppSearchResult>('apps', clean(query))
  }

  getApp(slug: string) {
    return this.get<AppDetail>(`apps/${encodeURIComponent(slug)}`)
  }

  getSimilar(slug: string) {
    return this.get<AppCardDto[]>(`apps/${encodeURIComponent(slug)}/similar`)
  }

  getLineage(slug: string) {
    return this.get<LineageNode>(`apps/${encodeURIComponent(slug)}/lineage`)
  }

  getUploader(username: string) {
    return this.get<UploaderDto>(`uploaders/${encodeURIComponent(username)}`)
  }

  getPublicCollections(username: string) {
    return this.get<CollectionDto[]>(`uploaders/${encodeURIComponent(username)}/collections`)
  }

  getPublicCollection(username: string, slug: string) {
    return this.get<CollectionDto>(`uploaders/${encodeURIComponent(username)}/collections/${encodeURIComponent(slug)}`)
  }
}
