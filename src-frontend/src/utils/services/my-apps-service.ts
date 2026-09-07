import { BaseService } from './base-service'
import type { OkDto } from '@/utils/models/common-models'
import type { AppCardDto, AppFileDto, ScreenshotDto } from '@/utils/models/catalog-models'
import type {
  AddExternalFileRequest,
  AppDraft,
  DuplicateCheck,
  ImportReleaseAssetRequest,
  MetadataSuggestion,
  MyAppStats,
  RepositoryInspection,
  SaveDraftRequest,
  SaveVersionRequest,
} from '@/utils/models/apps-models'

/** api/v1/my/apps — the upload wizard and "my apps" editor. */
export class MyAppsService extends BaseService {
  constructor() {
    super('my/apps')
  }

  list() {
    return this.get<AppCardDto[]>('')
  }

  create(request?: SaveDraftRequest) {
    return this.post<AppDraft>('', request ?? {})
  }

  getDraft(id: number) {
    return this.get<AppDraft>(`${id}`)
  }

  update(id: number, request: SaveDraftRequest) {
    return this.put<AppDraft>(`${id}`, request)
  }

  remove(id: number) {
    return this.delete<OkDto>(`${id}`)
  }

  stats(id: number) {
    return this.get<MyAppStats>(`${id}/stats`)
  }

  submit(id: number) {
    return this.post<AppDraft>(`${id}/submit`)
  }

  withdraw(id: number) {
    return this.post<AppDraft>(`${id}/withdraw`)
  }

  unlist(id: number) {
    return this.post<AppDraft>(`${id}/unlist`)
  }

  relist(id: number) {
    return this.post<AppDraft>(`${id}/relist`)
  }

  inspectRepository(repoUrl: string) {
    return this.post<RepositoryInspection>('inspect-repository', { repoUrl })
  }

  attachRepository(id: number, repoUrl: string, sourceRef?: string | null, prefill = true) {
    return this.post<AppDraft>(`${id}/source/repository`, { repoUrl, sourceRef, prefill })
  }

  uploadArchive(id: number, file: File, onProgress?: (p: number) => void) {
    const form = new FormData()
    form.append('file', file)
    return this.upload<AppDraft>(`${id}/source/archive`, form, onProgress)
  }

  removeSource(id: number) {
    return this.delete<AppDraft>(`${id}/source`)
  }

  uploadInstaller(id: number, versionId: number, platformCode: string, file: File, installHint?: string, onProgress?: (p: number) => void) {
    const form = new FormData()
    form.append('platformCode', platformCode)
    form.append('file', file)
    if (installHint) form.append('installHint', installHint)
    return this.upload<AppFileDto>(`${id}/versions/${versionId}/files`, form, onProgress)
  }

  addExternalFile(id: number, versionId: number, request: AddExternalFileRequest) {
    return this.post<AppFileDto>(`${id}/versions/${versionId}/files/external`, request)
  }

  importReleaseAsset(id: number, versionId: number, request: ImportReleaseAssetRequest) {
    return this.post<AppFileDto>(`${id}/versions/${versionId}/files/import-asset`, request, { timeout: 0 })
  }

  updateFile(id: number, fileId: number, platformCode?: string | null, installHint?: string | null) {
    return this.put<AppFileDto>(`${id}/files/${fileId}`, { platformCode, installHint })
  }

  deleteFile(id: number, fileId: number) {
    return this.delete<OkDto>(`${id}/files/${fileId}`)
  }

  createVersion(id: number, request: SaveVersionRequest) {
    return this.post<AppDraft>(`${id}/versions`, request)
  }

  updateVersion(id: number, versionId: number, request: SaveVersionRequest) {
    return this.put<AppDraft>(`${id}/versions/${versionId}`, request)
  }

  deleteVersion(id: number, versionId: number) {
    return this.delete<AppDraft>(`${id}/versions/${versionId}`)
  }

  submitVersion(id: number, versionId: number) {
    return this.post<AppDraft>(`${id}/versions/${versionId}/submit`)
  }

  uploadScreenshot(id: number, file: File, caption?: string, onProgress?: (p: number) => void) {
    const form = new FormData()
    form.append('file', file)
    if (caption) form.append('caption', caption)
    return this.upload<ScreenshotDto>(`${id}/screenshots`, form, onProgress)
  }

  deleteScreenshot(id: number, screenshotId: number) {
    return this.delete<OkDto>(`${id}/screenshots/${screenshotId}`)
  }

  reorderScreenshots(id: number, ids: number[]) {
    return this.post<OkDto>(`${id}/screenshots/reorder`, { ids })
  }

  setCaption(id: number, screenshotId: number, caption: string | null) {
    return this.put<OkDto>(`${id}/screenshots/${screenshotId}`, { caption })
  }

  uploadIcon(id: number, file: File) {
    const form = new FormData()
    form.append('file', file)
    return this.upload<string>(`${id}/icon`, form)
  }

  deleteIcon(id: number) {
    return this.delete<OkDto>(`${id}/icon`)
  }

  checkDuplicates(name: string, shortDescription: string, longDescription: string, excludeAppId?: number) {
    return this.post<DuplicateCheck>('check-duplicates', { name, shortDescription, longDescription, excludeAppId })
  }

  suggestMetadata(name: string, shortDescription: string, longDescription: string, readme?: string | null) {
    return this.post<MetadataSuggestion>('suggest-metadata', { name, shortDescription, longDescription, readme }, { timeout: 0 })
  }
}
