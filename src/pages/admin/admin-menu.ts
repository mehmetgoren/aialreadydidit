import type { MenuItem } from '@/utils/models/common-models'

/**
 * Admin drawer fallback (the API serves the same structure filtered by role → menus). Labels are i18n keys.
 * Every route listed here must exist in src/router/routes/admin/<area>.ts.
 */
export const ADMIN_MENU: MenuItem[] = [
  { label: 'admin_dashboard', icon: 'Odometer', route: '/admin' },
  {
    label: 'adm_group_moderation',
    icon: 'Stamp',
    children: [
      { label: 'adm_moderation_queue', icon: 'Checked', route: '/admin/moderation' },
      { label: 'adm_reports', icon: 'WarningFilled', route: '/admin/reports' },
      { label: 'adm_requests', icon: 'QuestionFilled', route: '/admin/requests' },
    ],
  },
  {
    label: 'adm_group_catalog',
    icon: 'Goods',
    children: [
      { label: 'adm_apps', icon: 'Box', route: '/admin/apps' },
      { label: 'admin_categories', icon: 'Files', route: '/admin/categories' },
      { label: 'adm_tags', icon: 'CollectionTag', route: '/admin/tags' },
      { label: 'adm_licenses', icon: 'Document', route: '/admin/licenses' },
      { label: 'adm_platforms', icon: 'Monitor', route: '/admin/platforms' },
      { label: 'adm_llm_models', icon: 'MagicStick', route: '/admin/llm-models' },
    ],
  },
  {
    label: 'adm_group_members',
    icon: 'UserFilled',
    children: [
      { label: 'app_users', icon: 'User', route: '/admin/users' },
      { label: 'adm_api_keys', icon: 'Key', route: '/admin/api-keys' },
      { label: 'adm_identity_sessions', icon: 'Monitor', route: '/admin/sessions' },
      { label: 'adm_identity_roles', icon: 'Avatar', route: '/admin/roles' },
      { label: 'adm_identity_menus', icon: 'Menu', route: '/admin/menus' },
      { label: 'adm_identity_role_menus', icon: 'Link', route: '/admin/role-menus' },
      { label: 'adm_identity_role_actions', icon: 'Operation', route: '/admin/role-actions' },
    ],
  },
  {
    label: 'adm_group_content',
    icon: 'Picture',
    children: [
      { label: 'adm_content_featured', icon: 'Star', route: '/admin/featured' },
      { label: 'adm_content_banners', icon: 'Picture', route: '/admin/banners' },
    ],
  },
  {
    label: 'adm_group_stats',
    icon: 'DataLine',
    children: [
      { label: 'adm_stats_overview', icon: 'DataLine', route: '/admin/stats' },
      { label: 'adm_stats_search', icon: 'Search', route: '/admin/search-analytics' },
      { label: 'adm_stats_savings', icon: 'Coin', route: '/admin/savings' },
    ],
  },
  {
    label: 'adm_group_system',
    icon: 'Setting',
    children: [
      { label: 'adm_system_settings', icon: 'Setting', route: '/admin/settings' },
      { label: 'adm_identity_audit_log', icon: 'Document', route: '/admin/audit-log' },
      { label: 'adm_system_jobs', icon: 'Timer', route: '/admin/jobs' },
      { label: 'adm_system_health', icon: 'FirstAidKit', route: '/admin/health' },
    ],
  },
]
