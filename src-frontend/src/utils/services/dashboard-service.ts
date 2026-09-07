import { BaseService, clean } from './base-service'
import type { OkDto, Paged } from '@/utils/models/common-models'
import type { AppCardDto, CollectionDto, RatingDto } from '@/utils/models/catalog-models'
import type { ApiKeyDto, CreateApiKeyRequest, DashboardOverview, DownloadHistoryDto, NotificationDto, SaveCollectionRequest, WatchDto } from '@/utils/models/dashboard-models'

/** api/v1/my — member dashboard. */
export class DashboardService extends BaseService {
  constructor() {
    super('my')
  }

  overview() {
    return this.get<DashboardOverview>('overview')
  }

  downloads(page = 1, pageSize = 20) {
    return this.get<Paged<DownloadHistoryDto>>('downloads', { page, pageSize })
  }

  ratings() {
    return this.get<RatingDto[]>('ratings')
  }

  favorites() {
    return this.get<AppCardDto[]>('favorites')
  }

  toggleFavorite(appId: number) {
    return this.post<boolean>(`favorites/${appId}/toggle`)
  }

  collections() {
    return this.get<CollectionDto[]>('collections')
  }

  collection(id: number) {
    return this.get<CollectionDto>(`collections/${id}`)
  }

  createCollection(request: SaveCollectionRequest) {
    return this.post<CollectionDto>('collections', request)
  }

  updateCollection(id: number, request: SaveCollectionRequest) {
    return this.put<CollectionDto>(`collections/${id}`, request)
  }

  deleteCollection(id: number) {
    return this.delete<OkDto>(`collections/${id}`)
  }

  addToCollection(id: number, appId: number, note?: string) {
    return this.post<CollectionDto>(`collections/${id}/items`, { appId, note })
  }

  removeFromCollection(id: number, appId: number) {
    return this.delete<CollectionDto>(`collections/${id}/items/${appId}`)
  }

  watches() {
    return this.get<WatchDto[]>('watches')
  }

  watch(appId: number, notifyNewVersion = true, notifyReplies = true) {
    return this.put<boolean>(`watches/${appId}`, { notifyNewVersion, notifyReplies })
  }

  unwatch(appId: number) {
    return this.delete<boolean>(`watches/${appId}`)
  }

  notifications(unread = false, page = 1, pageSize = 20) {
    return this.get<Paged<NotificationDto>>('notifications', clean({ unread, page, pageSize }))
  }

  markAllRead() {
    return this.post<OkDto>('notifications/read')
  }

  markRead(id: number) {
    return this.post<OkDto>(`notifications/${id}/read`)
  }

  apiKeys() {
    return this.get<ApiKeyDto[]>('api-keys')
  }

  createApiKey(request: CreateApiKeyRequest) {
    return this.post<ApiKeyDto>('api-keys', request)
  }

  revokeApiKey(id: number) {
    return this.delete<OkDto>(`api-keys/${id}`)
  }
}
