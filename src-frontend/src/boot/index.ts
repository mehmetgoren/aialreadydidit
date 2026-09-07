import type { App } from 'vue'
import { createPinia } from 'pinia'
import * as ElementPlusIconsVue from '@element-plus/icons-vue'
import { applyDocumentLocale, i18n, loadLocaleMessages, type AppLocale } from './i18n'
import { applyTheme } from './theme'
import router from '@/router'

/** Installs every boot module (prototype: quasar boot files) and loads the initial language chunk. */
export async function bootApp(app: App) {
  for (const [key, component] of Object.entries(ElementPlusIconsVue)) app.component(key, component)
  app.use(createPinia())
  app.use(i18n)
  app.use(router)
  applyTheme()
  applyDocumentLocale(String(i18n.global.locale.value))
  await loadLocaleMessages(i18n.global.locale.value as AppLocale).catch(() => {}) // English fallback still renders
}
