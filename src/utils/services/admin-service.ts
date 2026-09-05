import { BaseService, clean } from './base-service'
import type { AdminListQuery, OkDto, Paged } from '@/utils/models/common-models'
import type { AppDraft } from '@/utils/models/apps-models'
import type {
  AdminAppRow,
  AdminAppUpdateRequest,
  AdminApiKeyRow,
  AdminBannerDto,
  AdminCategoryNode,
  AdminDashboard,
  AdminLicenseDto,
  AdminLlmModelDto,
  AdminMenuItemDto,
  AdminPlatformDto,
  AdminReportDto,
  AdminRequestDto,
  AdminSessionDto,
  AdminTagDto,
  AdminUpdateUserRequest,
  AdminUserDetail,
  AdminUserRow,
  AuditLogDto,
  ControllerActionsDto,
  FeaturedItem,
  JobDto,
  MenuDto,
  ModerationDecisionRequest,
  ModerationDetail,
  ModerationQueueItem,
  ResolveReportRequest,
  RoleActionDto,
  RoleDto,
  RoleMenuDto,
  SaveCategoryRequest,
  SavingsBreakdown,
  SearchAnalytics,
  SiteSettingDto,
  StatsOverview,
  SystemHealth,
} from '@/utils/models/admin-models'

type Q = AdminListQuery

/** api/v1/admin/panel — menu + dashboard. */
export class AdminPanelService extends BaseService {
  constructor() {
    super('admin/panel')
  }
  menu() {
    return this.get<AdminMenuItemDto[]>('menu')
  }
  dashboard() {
    return this.get<AdminDashboard>('dashboard')
  }
}

/** api/v1/admin/moderation */
export class AdminModerationService extends BaseService {
  constructor() {
    super('admin/moderation')
  }
  queue(query: Q) {
    return this.get<Paged<ModerationQueueItem>>('', clean(query))
  }
  detail(appId: number) {
    return this.get<ModerationDetail>(`${appId}`)
  }
  approve(appId: number, request: ModerationDecisionRequest) {
    return this.post<ModerationDetail>(`${appId}/approve`, request)
  }
  reject(appId: number, request: ModerationDecisionRequest) {
    return this.post<ModerationDetail>(`${appId}/reject`, request)
  }
  rescan(appId: number, versionId?: number | null) {
    return this.post<ModerationDetail>(`${appId}/rescan`, undefined, { params: clean({ versionId }) })
  }
  verifyLicense(appId: number, note?: string) {
    return this.post<ModerationDetail>(`${appId}/verify-license`, { note })
  }
  note(appId: number, note: string, versionId?: number | null) {
    return this.post<ModerationDetail>(`${appId}/note`, { note, versionId })
  }
}

/** api/v1/admin/reports */
export class AdminReportsService extends BaseService {
  constructor() {
    super('admin/reports')
  }
  list(query: Q) {
    return this.get<Paged<AdminReportDto>>('', clean(query))
  }
  getOne(id: number) {
    return this.get<AdminReportDto>(`${id}`)
  }
  resolve(id: number, request: ResolveReportRequest) {
    return this.post<AdminReportDto>(`${id}/resolve`, request)
  }
}

/** api/v1/admin/requests */
export class AdminRequestsService extends BaseService {
  constructor() {
    super('admin/requests')
  }
  list(query: Q) {
    return this.get<Paged<AdminRequestDto>>('', clean(query))
  }
  setStatus(id: number, status: string, appId?: number | null) {
    return this.post<OkDto>(`${id}/status`, undefined, { params: clean({ status, appId }) })
  }
  remove(id: number) {
    return this.delete<OkDto>(`${id}`)
  }
}

/** api/v1/admin/apps */
export class AdminAppsService extends BaseService {
  constructor() {
    super('admin/apps')
  }
  list(query: Q & { category?: string; uploader?: string }) {
    return this.get<Paged<AdminAppRow>>('', clean(query))
  }
  getOne(id: number) {
    return this.get<AppDraft>(`${id}`)
  }
  update(id: number, request: AdminAppUpdateRequest) {
    return this.put<AppDraft>(`${id}`, request)
  }
  action(id: number, action: 'unlist' | 'restore' | 'remove' | 'feature' | 'unfeature', note?: string) {
    return this.post<AppDraft>(`${id}/${action}`, undefined, { params: clean({ note }) })
  }
  recompute(id: number) {
    return this.post<OkDto>(`${id}/recompute`)
  }
  remove(id: number) {
    return this.delete<OkDto>(`${id}`)
  }
}

/** api/v1/admin/categories | tags | licenses | platforms | llm-models */
export class AdminCatalogService extends BaseService {
  constructor() {
    super('admin')
  }
  categoryTree() {
    return this.get<AdminCategoryNode[]>('categories/tree')
  }
  createCategory(request: SaveCategoryRequest) {
    return this.post<AdminCategoryNode>('categories', request)
  }
  updateCategory(id: number, request: SaveCategoryRequest) {
    return this.put<AdminCategoryNode>(`categories/${id}`, request)
  }
  approveCategory(id: number) {
    return this.post<AdminCategoryNode>(`categories/${id}/approve`)
  }
  moveCategory(id: number, newParentId: number | null) {
    return this.post<AdminCategoryNode>(`categories/${id}/move`, { newParentId })
  }
  reorderCategories(orderedIds: number[]) {
    return this.post<OkDto>('categories/reorder', { orderedIds })
  }
  mergeCategory(id: number, targetId: number) {
    return this.post<OkDto>(`categories/${id}/merge`, { targetId })
  }
  deleteCategory(id: number) {
    return this.delete<OkDto>(`categories/${id}`)
  }
  tags(query: Q) {
    return this.get<Paged<AdminTagDto>>('tags', clean(query))
  }
  updateTag(id: number, name: string, isBlocked: boolean) {
    return this.put<AdminTagDto>(`tags/${id}`, { name, isBlocked })
  }
  mergeTag(id: number, targetId: number) {
    return this.post<OkDto>(`tags/${id}/merge`, { targetId })
  }
  deleteTag(id: number) {
    return this.delete<OkDto>(`tags/${id}`)
  }
  licenses() {
    return this.get<AdminLicenseDto[]>('licenses')
  }
  saveLicense(id: number | null, request: Partial<AdminLicenseDto>) {
    return id ? this.put<AdminLicenseDto>(`licenses/${id}`, request) : this.post<AdminLicenseDto>('licenses', request)
  }
  deleteLicense(id: number) {
    return this.delete<OkDto>(`licenses/${id}`)
  }
  platforms() {
    return this.get<AdminPlatformDto[]>('platforms')
  }
  savePlatform(id: number | null, request: Partial<AdminPlatformDto>) {
    return id ? this.put<AdminPlatformDto>(`platforms/${id}`, request) : this.post<AdminPlatformDto>('platforms', request)
  }
  llmModels() {
    return this.get<AdminLlmModelDto[]>('llm-models')
  }
  saveLlmModel(id: number | null, request: Partial<AdminLlmModelDto>) {
    return id ? this.put<AdminLlmModelDto>(`llm-models/${id}`, request) : this.post<AdminLlmModelDto>('llm-models', request)
  }
  deleteLlmModel(id: number) {
    return this.delete<OkDto>(`llm-models/${id}`)
  }
}

/** api/v1/admin/users | api-keys | sessions | identity */
export class AdminIdentityService extends BaseService {
  constructor() {
    super('admin')
  }
  users(query: Q & { role?: string }) {
    return this.get<Paged<AdminUserRow>>('users', clean(query))
  }
  user(id: number) {
    return this.get<AdminUserDetail>(`users/${id}`)
  }
  updateUser(id: number, request: AdminUpdateUserRequest) {
    return this.put<AdminUserDetail>(`users/${id}`, request)
  }
  ban(id: number, banned: boolean, reason?: string, unlistApps = true) {
    return this.post<AdminUserDetail>(`users/${id}/ban`, { banned, reason, unlistApps })
  }
  revokeUserSessions(id: number) {
    return this.delete<OkDto>(`users/${id}/sessions`)
  }
  apiKeys(query: Q) {
    return this.get<Paged<AdminApiKeyRow>>('api-keys', clean(query))
  }
  revokeApiKey(id: number) {
    return this.post<OkDto>(`api-keys/${id}/revoke`)
  }
  setTier(id: number, rateTier: string) {
    return this.post<OkDto>(`api-keys/${id}/tier`, { rateTier })
  }
  sessions(query: Q) {
    return this.get<Paged<AdminSessionDto>>('sessions', clean(query))
  }
  revokeSession(id: number) {
    return this.delete<OkDto>(`sessions/${id}`)
  }
  roles() {
    return this.get<RoleDto[]>('identity/roles')
  }
  saveRole(id: number | null, name: string, isAdmin: boolean) {
    return id ? this.put<RoleDto>(`identity/roles/${id}`, { name, isAdmin }) : this.post<RoleDto>('identity/roles', { name, isAdmin })
  }
  deleteRole(id: number) {
    return this.delete<OkDto>(`identity/roles/${id}`)
  }
  menus() {
    return this.get<MenuDto[]>('identity/menus')
  }
  saveMenu(id: number | null, request: Partial<MenuDto>) {
    return id ? this.put<MenuDto>(`identity/menus/${id}`, request) : this.post<MenuDto>('identity/menus', request)
  }
  deleteMenu(id: number) {
    return this.delete<OkDto>(`identity/menus/${id}`)
  }
  roleMenus(roleId: number) {
    return this.get<RoleMenuDto[]>(`identity/roles/${roleId}/menus`)
  }
  saveRoleMenus(roleId: number, menuIds: number[]) {
    return this.put<OkDto>(`identity/roles/${roleId}/menus`, { menuIds })
  }
  roleActions(roleId: number) {
    return this.get<RoleActionDto[]>(`identity/roles/${roleId}/actions`)
  }
  saveRoleActions(roleId: number, actions: RoleActionDto[]) {
    return this.put<OkDto>(`identity/roles/${roleId}/actions`, { actions })
  }
  controllerActions() {
    return this.get<ControllerActionsDto[]>('identity/controller-actions')
  }
}

/** api/v1/admin/featured | banners */
export class AdminContentService extends BaseService {
  constructor() {
    super('admin')
  }
  featured() {
    return this.get<FeaturedItem[]>('featured')
  }
  feature(appId: number, note?: string) {
    return this.post<FeaturedItem[]>('featured', { appId, note })
  }
  reorderFeatured(orderedAppIds: number[]) {
    return this.post<FeaturedItem[]>('featured/reorder', { orderedAppIds })
  }
  unfeature(appId: number) {
    return this.delete<FeaturedItem[]>(`featured/${appId}`)
  }
  banners() {
    return this.get<AdminBannerDto[]>('banners')
  }
  saveBanner(id: number | null, request: Partial<AdminBannerDto>) {
    return id ? this.put<AdminBannerDto>(`banners/${id}`, request) : this.post<AdminBannerDto>('banners', request)
  }
  uploadBannerImage(id: number, file: File) {
    const form = new FormData()
    form.append('file', file)
    return this.upload<AdminBannerDto>(`banners/${id}/image`, form)
  }
  deleteBanner(id: number) {
    return this.delete<OkDto>(`banners/${id}`)
  }
}

/** api/v1/admin/stats | settings | audit-log | jobs | health */
export class AdminSystemService extends BaseService {
  constructor() {
    super('admin')
  }
  stats(days = 30) {
    return this.get<StatsOverview>('stats', { days })
  }
  searchAnalytics(days = 30) {
    return this.get<SearchAnalytics>('stats/search', { days })
  }
  savings() {
    return this.get<SavingsBreakdown>('stats/savings')
  }
  settings() {
    return this.get<SiteSettingDto[]>('settings')
  }
  saveSettings(values: Record<string, string | null>) {
    return this.put<SiteSettingDto[]>('settings', { values })
  }
  audit(query: Q) {
    return this.get<Paged<AuditLogDto>>('audit-log', clean(query))
  }
  jobs(query: Q) {
    return this.get<Paged<JobDto>>('jobs', clean(query))
  }
  retryJob(id: number) {
    return this.post<OkDto>(`jobs/${id}/retry`)
  }
  cancelJob(id: number) {
    return this.post<OkDto>(`jobs/${id}/cancel`)
  }
  maintenance(task: string) {
    return this.post<string>(`jobs/maintenance/${task}`)
  }
  health() {
    return this.get<SystemHealth>('health')
  }
}
