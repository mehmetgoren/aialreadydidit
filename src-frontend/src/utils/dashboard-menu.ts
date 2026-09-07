import type { MenuItem } from '@/utils/models/common-models'

/** Member dashboard left menu (prototype: profile-menu). Labels are i18n keys. */
export const DASHBOARD_MENU: MenuItem[] = [
  { label: 'dash_overview', icon: 'Odometer', route: '/dashboard' },
  { label: 'dash_my_apps', icon: 'Box', route: '/dashboard/apps' },
  { label: 'dash_new_app', icon: 'Upload', route: '/upload' },
  { label: 'dash_downloads', icon: 'Download', route: '/dashboard/downloads' },
  { label: 'dash_ratings', icon: 'ChatLineSquare', route: '/dashboard/ratings' },
  { label: 'dash_favorites', icon: 'Star', route: '/dashboard/favorites' },
  { label: 'dash_collections', icon: 'Collection', route: '/dashboard/collections' },
  { label: 'dash_watches', icon: 'Bell', route: '/dashboard/watches' },
  { label: 'dash_notifications', icon: 'Message', route: '/dashboard/notifications' },
  { label: 'dash_api_keys', icon: 'Key', route: '/dashboard/api-keys' },
  { label: 'dash_settings', icon: 'Setting', route: '/dashboard/settings' },
]
