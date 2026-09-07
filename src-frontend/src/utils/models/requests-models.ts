import type { AppCardDto } from './catalog-models'

export type AppRequestStatus = 'open' | 'fulfilled' | 'closed'

export interface AppRequestDto {
  id: number
  title: string
  description: string
  requesterUsername: string | null
  source: string
  status: AppRequestStatus
  fulfilledBy: AppCardDto | null
  voteCount: number
  myVote: boolean
  createdAt: string
  suggestions: AppCardDto[]
}

export interface AgentFile {
  fileId: number
  platform: string | null
  kind: string
  fileName: string
  sizeBytes: number
  downloadUrl: string
  externalReference: string | null
}

export interface AgentMatch {
  slug: string
  name: string
  shortDescription: string
  similarity: number | null
  url: string
  apiUrl: string
  license: string
  generatedBy: string | null
  platforms: string[]
  ratingAvg: number
  ratingCount: number
  downloadCount: number
  repoUrl: string | null
  latestVersion: string | null
  files: AgentFile[]
  estGenerationTokens: number
}

export interface AgentCheck {
  verdict: 'download' | 'fork' | 'build'
  advice: string
  bestSimilarity: number | null
  semanticSearchUsed: boolean
  matches: AgentMatch[]
  requestUrl: string
  estimatedTokensIfBuilt: number
}

export interface AgentDocs {
  name: string
  mission: string
  flow: string
  apiBaseUrl: string
  openApiUrl: string
  mcpUrl: string
  authentication: string
  endpoints: Record<string, string>
  rateLimits: string
  publishedApps: number
}
