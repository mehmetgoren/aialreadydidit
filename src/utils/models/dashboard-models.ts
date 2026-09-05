import type { AppCardDto, RatingDto } from './catalog-models'

export interface DownloadHistoryDto {
  id: number
  app: AppCardDto
  version: string
  fileName: string
  platformCode: string | null
  fileId: number
  source: string
  createdAt: string
  rated: boolean
}

export type NotificationType = 'newVersion' | 'moderationApproved' | 'moderationRejected' | 'reviewReply' | 'newReview' | 'reportResolved' | 'system' | 'scanInfected' | 'requestFulfilled'

export interface NotificationDto {
  id: number
  type: NotificationType
  title: string
  body: string | null
  link: string | null
  readAt: string | null
  createdAt: string
}

export interface ApiKeyDto {
  id: number
  name: string
  prefix: string
  scopes: string
  rateTier: string
  lastUsedAt: string | null
  expiresAt: string | null
  revokedAt: string | null
  requestCount: number
  downloadCount: number
  createdAt: string
  secret: string | null
}

export interface CreateApiKeyRequest {
  name: string
  scopes?: string[]
  expiresInDays?: number | null
}

export interface WatchDto {
  id: number
  app: AppCardDto
  notifyNewVersion: boolean
  notifyReplies: boolean
  createdAt: string
}

export interface DashboardOverview {
  appCount: number
  publishedAppCount: number
  pendingAppCount: number
  totalDownloads: number
  tokensSavedByMyApps: number
  costSavedByMyApps: number
  ratingCount: number
  favoriteCount: number
  collectionCount: number
  watchCount: number
  unreadNotifications: number
  apiKeyCount: number
  recentNotifications: NotificationDto[]
  recentApps: AppCardDto[]
}

export interface SaveCollectionRequest {
  name: string
  description?: string | null
  isPublic: boolean
}

export type { RatingDto }
