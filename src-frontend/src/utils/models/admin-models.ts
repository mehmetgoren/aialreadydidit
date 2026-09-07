import type { AppCardDto, AppDetail, AppStatus, CategoryNode, SavingsDto, ScanStatus, VersionStatus, FacetItem } from './catalog-models'
import type { AppDraft, SaveDraftRequest } from './apps-models'
import type { ApiKeyDto } from './dashboard-models'

export interface AdminMenuItemDto {
  label: string
  icon: string | null
  route: string | null
  children: AdminMenuItemDto[]
}

export interface DailyPoint {
  date: string
  value: number
}

export interface ModerationQueueItem {
  appId: number
  versionId: number | null
  slug: string
  name: string
  shortDescription: string
  iconUrl: string | null
  coverUrl: string | null
  kind: 'app' | 'version'
  version: string | null
  appStatus: AppStatus
  versionStatus: VersionStatus | null
  uploaderUsername: string
  uploaderTrustLevel: number
  categoryName: string
  licenseSpdxId: string
  licenseOk: boolean
  fileCount: number
  infectedCount: number
  pendingScanCount: number
  screenshotCount: number
  submittedAt: string
  openReports: number
}

export interface ModerationActionDto {
  id: number
  versionId: number | null
  adminUsername: string
  action: string
  note: string | null
  createdAt: string
}

export interface ScanResultDto {
  id: number
  fileId: number
  fileName: string
  engine: string
  verdict: ScanStatus
  signature: string | null
  raw: string | null
  scannedAt: string
}

export interface AdminUserSummary {
  id: number
  username: string
  displayName: string
  email: string
  emailVerified: boolean
  role: string
  trustLevel: number
  isBanned: boolean
  isActive: boolean
  appCount: number
  publishedAppCount: number
  rejectedAppCount: number
  reportCount: number
  createdAt: string
  lastLoginAt: string | null
}

export interface AdminReportDto {
  id: number
  appId: number | null
  appSlug: string | null
  appName: string | null
  appStatus: AppStatus | null
  ratingId: number | null
  ratingReview: string | null
  reporterUsername: string | null
  reporterEmail: string | null
  reason: string
  details: string | null
  status: 'open' | 'reviewing' | 'resolved' | 'dismissed'
  handledBy: string | null
  resolution: string | null
  createdAt: string
  resolvedAt: string | null
}

export interface ModerationDetail {
  draft: AppDraft
  preview: AppDetail
  history: ModerationActionDto[]
  scanResults: ScanResultDto[]
  similarApps: AppCardDto[]
  reports: AdminReportDto[]
  llmSuggestedCategory: CategoryNode | null
  llmSuggestedCategoryProposedName: string | null
  licenseIssue: string | null
  uploader: AdminUserSummary
}

export interface ModerationDecisionRequest {
  note?: string | null
  versionId?: number | null
  categoryId?: number | null
  feature?: boolean
}

export interface AdminAppRow extends AppCardDto {
  uploaderEmail: string
  versionCount: number
  openReports: number
  createdAt: string
  submittedAt: string | null
  embeddingStale: boolean
  hasEmbedding: boolean
  viewCount: number
}

export interface AdminAppUpdateRequest extends SaveDraftRequest {
  isFeatured?: boolean | null
  featuredOrder?: number | null
  featuredNote?: string | null
  licenseVerifiedByAdmin?: boolean | null
  slug?: string | null
}

export interface ResolveReportRequest {
  status: 'open' | 'reviewing' | 'resolved' | 'dismissed'
  resolution?: string | null
  action?: 'none' | 'unlist' | 'remove' | 'hide_review' | 'ban_uploader'
}

export interface AdminRequestDto {
  id: number
  title: string
  description: string
  requesterUsername: string | null
  source: string
  status: 'open' | 'fulfilled' | 'closed'
  fulfilledByAppName: string | null
  fulfilledByAppSlug: string | null
  voteCount: number
  createdAt: string
}

export interface AdminCategoryNode {
  id: number
  parentId: number | null
  level: number
  slug: string
  nameEn: string
  nameTr: string
  description: string | null
  icon: string | null
  sortOrder: number
  isActive: boolean
  isLlmProposed: boolean
  appCount: number
  totalAppCount: number
  children: AdminCategoryNode[]
}

export interface SaveCategoryRequest {
  nameEn: string
  nameTr?: string | null
  slug?: string | null
  description?: string | null
  icon?: string | null
  parentId?: number | null
  isActive: boolean
}

export interface AdminTagDto {
  id: number
  name: string
  slug: string
  usageCount: number
  isBlocked: boolean
}

export interface AdminLicenseDto {
  id: number
  spdxId: string
  name: string
  url: string | null
  family: string | null
  isOsiApproved: boolean
  isFsfLibre: boolean
  isAllowed: boolean
  sortOrder: number
  appCount: number
}

export interface AdminPlatformDto {
  id: number
  code: string
  name: string
  icon: string | null
  allowedExtensions: string
  allowsExternalReference: boolean
  installHint: string | null
  sortOrder: number
  isActive: boolean
  fileCount: number
}

export interface AdminLlmModelDto {
  id: number
  vendor: string
  name: string
  version: string | null
  slug: string
  releasedOn: string | null
  isActive: boolean
  sortOrder: number
  appCount: number
}

export interface AdminUserRow {
  id: number
  username: string
  displayName: string
  email: string
  emailVerified: boolean
  role: string
  roleId: number
  trustLevel: number
  isActive: boolean
  isBanned: boolean
  avatarUrl: string | null
  appCount: number
  ratingCount: number
  downloadCount: number
  apiKeyCount: number
  hasGoogle: boolean
  createdAt: string
  lastLoginAt: string | null
}

export interface AdminSessionDto {
  id: number
  userId: number
  username: string
  createdAt: string
  lastUsedAt: string | null
  expiresAt: string
  ip: string | null
  userAgent: string | null
  revoked: boolean
}

export interface AdminUserDetail extends AdminUserRow {
  bio: string | null
  website: string | null
  banReason: string | null
  adminNote: string | null
  locale: string
  apps: AdminAppRow[]
  apiKeys: ApiKeyDto[]
  sessions: AdminSessionDto[]
  reportCountAgainst: number
  reportCountFiled: number
}

export interface AdminUpdateUserRequest {
  displayName?: string | null
  roleId?: number | null
  trustLevel?: number | null
  isActive?: boolean | null
  adminNote?: string | null
  emailVerified?: boolean | null
}

export interface AdminApiKeyRow extends ApiKeyDto {
  userId: number
  username: string
  email: string
}

export interface RoleDto {
  id: number
  name: string
  isAdmin: boolean
  userCount: number
}

export interface MenuDto {
  id: number
  name: string
  route: string | null
  description: string | null
  orderNum: number | null
  parentId: number | null
  visible: boolean
  icon: string | null
  children: MenuDto[]
}

export interface RoleMenuDto {
  menuId: number
  menuName: string
  route: string | null
  parentId: number | null
  hasAccess: boolean
}

export interface RoleActionDto {
  id?: number
  controller: string
  action: string
}

export interface ControllerActionsDto {
  controller: string
  actions: string[]
}

export interface AdminDashboard {
  pendingReview: number
  pendingScan: number
  openReports: number
  openRequests: number
  publishedApps: number
  totalApps: number
  members: number
  newMembers7d: number
  downloads: number
  downloads7d: number
  searches: number
  searches7d: number
  zeroResultSearches7d: number
  agentCalls7d: number
  failedJobs: number
  queuedJobs: number
  infectedFiles: number
  proposedCategories: number
  savings: SavingsDto
  downloadsSeries: DailyPoint[]
  searchesSeries: DailyPoint[]
  signupsSeries: DailyPoint[]
  topApps: AppCardDto[]
  queuePreview: ModerationQueueItem[]
  recentReports: AdminReportDto[]
}

export interface TopUploader {
  username: string
  displayName: string
  apps: number
  downloads: number
}

export interface StatsOverview {
  downloads: DailyPoint[]
  searches: DailyPoint[]
  signups: DailyPoint[]
  uploads: DailyPoint[]
  views: DailyPoint[]
  downloadsByPlatform: FacetItem[]
  downloadsBySource: FacetItem[]
  appsByCategory: FacetItem[]
  appsByModel: FacetItem[]
  appsByLicense: FacetItem[]
  topApps: AppCardDto[]
  topUploaders: TopUploader[]
}

export interface QueryStat {
  query: string
  count: number
  avgResults: number
  avgTopSimilarity: number | null
  lastSeen: string
}

export interface SearchAnalytics {
  topQueries: QueryStat[]
  zeroResultQueries: QueryStat[]
  bySource: FacetItem[]
  byMode: FacetItem[]
  avgTookMs: number
  total: number
  series: DailyPoint[]
}

export interface AppSavings {
  appId: number
  slug: string
  name: string
  downloads: number
  tokensPerDownload: number
  tokensSaved: number
  costSavedUsd: number
  isOverride: boolean
}

export interface SavingsBreakdown {
  totals: SavingsDto
  tokensSeries: DailyPoint[]
  topApps: AppSavings[]
  coefficients: Record<string, number>
}

export interface AuditLogDto {
  id: number
  userId: number | null
  username: string
  action: string
  entity: string | null
  entityId: string | null
  details: string | null
  ipAddress: string | null
  createdAt: string
}

export interface JobDto {
  id: number
  type: string
  payloadJson: string
  status: 'queued' | 'running' | 'done' | 'failed' | 'cancelled'
  attempts: number
  maxAttempts: number
  lastError: string | null
  runAt: string
  startedAt: string | null
  finishedAt: string | null
  createdAt: string
  subject: string | null
}

export interface SiteSettingDto {
  key: string
  value: string | null
  group: string
  valueType: string
  description: string | null
  updatedAt: string
}

export interface AdminBannerDto {
  id: number
  title: string
  subtitle: string | null
  imageUrl: string | null
  link: string | null
  position: string
  sortOrder: number
  isActive: boolean
  startsAt: string | null
  endsAt: string | null
  createdAt: string
}

export interface FeaturedItem extends AppCardDto {
  featuredOrder: number
  featuredNote: string | null
}

export interface HealthCheck {
  name: string
  ok: boolean
  detail: string | null
  latencyMs: number | null
}

export interface SystemHealth {
  checks: HealthCheck[]
  config: Record<string, string>
  version: string
  startedAt: string
  environment: string
}
