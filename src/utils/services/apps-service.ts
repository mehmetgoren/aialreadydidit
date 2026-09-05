import { BaseService, clean } from './base-service'
import type { OkDto, Paged } from '@/utils/models/common-models'
import type { CreateRatingRequest, CreateReportRequest, DownloadLink, RatingDto, RatingSummary } from '@/utils/models/catalog-models'

/** api/v1/apps/{slug}/... — download, ratings, report (public + member). */
export class AppsService extends BaseService {
  constructor() {
    super('apps')
  }

  /** Returns the time-limited link (the API records the download). */
  downloadLink(slug: string, fileId: number) {
    return this.get<DownloadLink>(`${encodeURIComponent(slug)}/download/${fileId}`, { json: 1 })
  }

  ratings(slug: string, sort: string, page: number, pageSize = 10) {
    return this.get<Paged<RatingDto>>(`${encodeURIComponent(slug)}/ratings`, clean({ sort, page, pageSize }))
  }

  ratingSummary(slug: string) {
    return this.get<RatingSummary>(`${encodeURIComponent(slug)}/ratings/summary`)
  }

  rate(slug: string, request: CreateRatingRequest) {
    return this.put<RatingDto>(`${encodeURIComponent(slug)}/ratings/mine`, request)
  }

  deleteRating(slug: string) {
    return this.delete<OkDto>(`${encodeURIComponent(slug)}/ratings/mine`)
  }

  vote(ratingId: number, helpful: boolean | null) {
    return this.post<RatingDto>(`ratings/${ratingId}/vote`, { helpful })
  }

  reply(ratingId: number, body: string) {
    return this.post<RatingDto>(`ratings/${ratingId}/reply`, { body })
  }

  report(slug: string, request: CreateReportRequest) {
    return this.post<number>(`${encodeURIComponent(slug)}/report`, request)
  }
}
