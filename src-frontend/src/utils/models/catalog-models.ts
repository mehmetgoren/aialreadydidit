import type { Paged } from './common-models'

export type AppStatus = 'draft' | 'pendingScan' | 'pendingReview' | 'published' | 'rejected' | 'unlisted' | 'removed'
export type VersionStatus = 'draft' | 'pendingScan' | 'pendingReview' | 'published' | 'rejected' | 'removed'
export type FileKind = 'installer' | 'source' | 'dockerImage' | 'webBundle'
export type ScanStatus = 'pending' | 'clean' | 'infected' | 'error' | 'skipped'
export type SourceKind = 'repository' | 'archive'
export type DerivationKind = 'fork' | 'inspired' | 'port'

export interface CategoryNode {
  id: number
  slug: string
  nameEn: string
  nameTr: string
  icon: string | null
  level: number
  parentId: number | null
  appCount: number
  children: CategoryNode[]
}

export interface CategoryDetail {
  node: CategoryNode
  ancestors: CategoryNode[]
  children: CategoryNode[]
}

export interface PlatformDto {
  id: number
  code: string
  name: string
  icon: string | null
  allowedExtensions: string
  allowsExternalReference: boolean
  installHint: string | null
}

export interface LicenseDto {
  id: number
  spdxId: string
  name: string
  url: string | null
  isOsiApproved: boolean
  isAllowed: boolean
  appCount: number
}

export interface LlmModelDto {
  id: number
  vendor: string
  name: string
  version: string | null
  slug: string
  displayName: string
  appCount: number
}

export interface TagDto {
  id: number
  name: string
  slug: string
  usageCount: number
}

export interface AppCardDto {
  id: number
  slug: string
  name: string
  shortDescription: string
  iconUrl: string | null
  coverUrl: string | null
  categorySlug: string
  categoryNameEn: string
  categoryNameTr: string
  licenseSpdxId: string
  llmModelName: string | null
  llmModelSlug: string | null
  platforms: string[]
  ratingAvg: number
  ratingCount: number
  downloadCount: number
  latestVersion: string | null
  publishedAt: string | null
  updatedAt: string
  uploaderUsername: string
  isFeatured: boolean
  estGenerationTokens: number
  estGenerationCostUsd: number
  similarity: number | null
  status: AppStatus
  derivedFromAppId: number | null
}

export interface AppQuery {
  q?: string
  mode?: 'hybrid' | 'keyword' | 'semantic'
  category?: string
  platform?: string
  license?: string
  model?: string
  minRating?: number
  tags?: string
  uploader?: string
  sort?: string
  page?: number
  pageSize?: number
  featured?: boolean
}

export interface FacetItem {
  value: string
  label: string
  count: number
}

export interface AppSearchResult {
  page: Paged<AppCardDto>
  platforms: FacetItem[]
  licenses: FacetItem[]
  models: FacetItem[]
  categories: FacetItem[]
  modeUsed: string
  semanticAvailable: boolean
}

export interface ScreenshotDto {
  id: number
  url: string
  thumbUrl: string
  width: number
  height: number
  caption: string | null
  sortOrder: number
}

export interface AppFileDto {
  id: number
  versionId: number
  platformCode: string | null
  platformName: string | null
  kind: FileKind
  fileName: string
  externalReference: string | null
  sizeBytes: number
  sha256: string | null
  contentType: string | null
  scanStatus: ScanStatus
  scanSignature: string | null
  installHint: string | null
  downloadCount: number
  downloadUrl: string
}

export interface AppVersionDto {
  id: number
  version: string
  changelog: string | null
  releasedAt: string
  sourceRef: string | null
  status: VersionStatus
  rejectionReason: string | null
  downloadCount: number
  files: AppFileDto[]
}

export interface AppPromptDto {
  id: number
  title: string
  promptText: string
  sortOrder: number
}

export interface UploaderDto {
  id: number
  username: string
  displayName: string
  avatarUrl: string | null
  bio: string | null
  website: string | null
  trustLevel: number
  appCount: number
  totalDownloads: number
  memberSince: string
}

export interface LineageNode {
  id: number
  slug: string
  name: string
  iconUrl: string | null
  derivationKind: DerivationKind | null
  derivatives: LineageNode[]
}

export interface ViewerState {
  isFavorite: boolean
  isWatching: boolean
  hasDownloaded: boolean
  canRate: boolean
  isOwner: boolean
  myRatingId: number | null
  collectionIds: number[]
}

export interface AppDetail {
  id: number
  slug: string
  name: string
  shortDescription: string
  longDescription: string
  readmeMarkdown: string | null
  iconUrl: string | null
  homepageUrl: string | null
  status: AppStatus
  rejectionReason: string | null
  category: CategoryNode
  categoryPath: CategoryNode[]
  license: LicenseDto
  llmModel: LlmModelDto | null
  llmModelNote: string | null
  uploader: UploaderDto
  sourceKind: SourceKind
  repoUrl: string | null
  repoProvider: string | null
  repoStars: number | null
  repoPrimaryLanguage: string | null
  repoSyncedAt: string | null
  platforms: string[]
  tags: string[]
  screenshots: ScreenshotDto[]
  versions: AppVersionDto[]
  latestVersion: AppVersionDto | null
  prompts: AppPromptDto[]
  derivedFrom: LineageNode | null
  derivationKind: DerivationKind | null
  derivatives: LineageNode[]
  ratingAvg: number
  ratingCount: number
  workedCount: number
  notWorkedCount: number
  downloadCount: number
  viewCount: number
  favoriteCount: number
  isFeatured: boolean
  estGenerationTokens: number
  estGenerationCostUsd: number
  estSavedCostUsd: number
  estSavedTokens: number
  sourceLineCount: number
  sourceFileCount: number
  publishedAt: string | null
  createdAt: string
  updatedAt: string
  viewer: ViewerState
}

export interface SavingsDto {
  tokensSaved: number
  costSavedUsd: number
  kwhSaved: number
  co2SavedKg: number
  totalDownloads: number
  /** Distinct (user, IP) downloaders summed over apps — what the counter multiplies. */
  uniqueDownloads: number
  /** Share of downloads assumed to replace a fresh generation (admin setting, 0–1). */
  reuseShare: number
  publishedApps: number
  computedAt: string
}

export interface BannerDto {
  id: number
  title: string
  subtitle: string | null
  imageUrl: string | null
  link: string | null
  position: string
}

export interface HomeStats {
  publishedApps: number
  members: number
  downloads: number
  searches: number
  agentSearches: number
}

export interface HomeContent {
  savings: SavingsDto
  featured: AppCardDto[]
  trending: AppCardDto[]
  newest: AppCardDto[]
  recentlyUpdated: AppCardDto[]
  topRated: AppCardDto[]
  categories: CategoryNode[]
  banners: BannerDto[]
  openRequestCount: number
  stats: HomeStats
}

export interface RatingReplyDto {
  id: number
  username: string
  displayName: string
  isUploader: boolean
  body: string
  createdAt: string
}

export interface RatingDto {
  id: number
  appId: number
  appSlug: string
  appName: string
  username: string
  displayName: string
  avatarUrl: string | null
  score: number
  review: string | null
  version: string | null
  worked: boolean | null
  helpfulCount: number
  myVote: boolean | null
  isMine: boolean
  status: 'visible' | 'hidden'
  createdAt: string
  updatedAt: string
  replies: RatingReplyDto[]
}

export interface RatingSummary {
  average: number
  count: number
  workedCount: number
  notWorkedCount: number
  histogram: number[]
}

export interface CollectionDto {
  id: number
  name: string
  slug: string
  description: string | null
  isPublic: boolean
  ownerUsername: string
  ownerDisplayName: string
  itemCount: number
  updatedAt: string
  items: AppCardDto[]
}

export interface DownloadLink {
  url: string
  fileName: string
  sizeBytes: number
  sha256: string | null
  contentType: string | null
  installHint: string | null
  externalReference: string | null
  expiresAt: string
  version: string
  platformCode: string | null
}

export type ReportReason = 'malware' | 'notOpenSource' | 'notFree' | 'copyright' | 'broken' | 'spam' | 'inappropriate' | 'other'

export interface CreateReportRequest {
  reason: ReportReason
  details?: string
  email?: string
  ratingId?: number | null
}

export interface CreateRatingRequest {
  score: number
  review?: string | null
  versionId?: number | null
  worked?: boolean | null
}
