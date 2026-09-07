import type { LicenseDto, LlmModelDto, PlatformDto } from './catalog-models'

export interface UploadLimits {
  minScreenshots: number
  maxScreenshots: number
  recommendedScreenshots: number
  maxTags: number
  maxInstallerBytes: number
  maxSourceArchiveBytes: number
  maxScreenshotBytes: number
  maxIconBytes: number
  duplicateThreshold: number
}

export interface SiteConfig {
  siteName: string
  publicUrl: string
  apiPublicUrl: string
  mcpPublicUrl: string
  defaultLocale: string
  googleClientId: string | null
  requireEmailVerification: boolean
  semanticSearchAvailable: boolean
  categorySuggestionsAvailable: boolean
  announcementText: string | null
  announcementLink: string | null
  contactEmail: string | null
  allowAnonymousReports: boolean
  uploadLimits: UploadLimits
  platforms: PlatformDto[]
  licenses: LicenseDto[]
  llmModels: LlmModelDto[]
}
