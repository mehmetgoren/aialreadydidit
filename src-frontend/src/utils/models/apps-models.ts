import type { AppCardDto, AppPromptDto, AppStatus, AppVersionDto, CategoryNode, DerivationKind, ScreenshotDto, SourceKind } from './catalog-models'

export interface PromptInput {
  title: string
  promptText: string
}

export interface SaveDraftRequest {
  name?: string
  shortDescription?: string
  longDescription?: string
  categoryId?: number | null
  licenseId?: number | null
  llmModelId?: number | null
  llmModelNote?: string | null
  homepageUrl?: string | null
  tags?: string[]
  derivedFromAppId?: number | null
  derivationKind?: DerivationKind | null
  prompts?: PromptInput[]
  estGenerationTokens?: number | null
  estGenerationCostUsd?: number | null
}

export interface ReleaseAssetDto {
  name: string
  url: string
  size: number
  suggestedPlatform: string | null
}

export interface ReleaseDto {
  tag: string
  name: string | null
  body: string | null
  publishedAt: string | null
  assets: ReleaseAssetDto[]
}

export interface RepositoryInspection {
  provider: string
  owner: string
  name: string
  url: string
  description: string | null
  homepage: string | null
  defaultBranch: string
  stars: number
  primaryLanguage: string | null
  licenseSpdxId: string | null
  licenseId: number | null
  detectedLicenseSpdxId: string | null
  hasReadme: boolean
  readmeExcerpt: string | null
  topics: string[]
  tags: string[]
  releases: ReleaseDto[]
  isArchived: boolean
  warnings: string[]
}

export interface SourceAnalysis {
  analyzedAt: string | null
  fileCount: number
  lineCount: number
  bytes: number
  hasLicenseFile: boolean
  detectedLicenseSpdxId: string | null
  primaryLanguage: string | null
  warnings: string | null
  licenseMatches: boolean
}

export interface ReadinessIssue {
  code: string
  message: string
  step: 'source' | 'details' | 'files' | 'screenshots'
  blocking: boolean
}

export interface Readiness {
  canSubmit: boolean
  issues: ReadinessIssue[]
}

export interface AppDraft {
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
  categoryId: number | null
  categoryPath: CategoryNode[]
  licenseId: number | null
  licenseSpdxId: string | null
  llmModelId: number | null
  llmModelNote: string | null
  tags: string[]
  derivedFromAppId: number | null
  derivedFromName: string | null
  derivedFromSlug: string | null
  derivationKind: DerivationKind | null
  prompts: AppPromptDto[]
  sourceKind: SourceKind | null
  repoUrl: string | null
  repoProvider: string | null
  repoDefaultBranch: string | null
  repoStars: number | null
  source: SourceAnalysis
  screenshots: ScreenshotDto[]
  versions: AppVersionDto[]
  draftVersion: AppVersionDto | null
  /** The live version — shown in the files step when there is no draft version. */
  publishedVersion: AppVersionDto | null
  /** Admin or trusted uploader: may add / remove install files of the published version. */
  canEditPublishedFiles: boolean
  estGenerationTokens: number
  estGenerationCostUsd: number
  estIsOverride: boolean
  readiness: Readiness
  downloadCount: number
  viewCount: number
  ratingCount: number
  ratingAvg: number
  createdAt: string
  updatedAt: string
  submittedAt: string | null
  publishedAt: string | null
  llmSuggestedCategoryId: number | null
}

export interface DailyCount {
  date: string
  count: number
}

export interface MyAppStats {
  appId: number
  downloads: DailyCount[]
  views: DailyCount[]
  downloadsByPlatform: Record<string, number>
  downloadsBySource: Record<string, number>
  estSavedTokens: number
  estSavedCostUsd: number
}

export interface DuplicateCheck {
  threshold: number
  semanticAvailable: boolean
  matches: AppCardDto[]
  bestMatch: AppCardDto | null
}

export interface MetadataSuggestion {
  available: boolean
  categoryId: number | null
  categorySlug: string | null
  categoryPath: string | null
  proposedCategoryName: string | null
  proposedCategoryParentSlug: string | null
  tags: string[]
  shortDescription: string | null
  reasoning: string | null
  model: string
}

export interface SaveVersionRequest {
  version: string
  changelog?: string | null
  releasedAt?: string | null
  sourceRef?: string | null
}

export interface AddExternalFileRequest {
  platformCode: string
  reference: string
  installHint?: string | null
}

export interface ImportReleaseAssetRequest {
  url: string
  platformCode: string
  installHint?: string | null
}
