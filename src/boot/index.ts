import type { App } from 'vue'
import { createPinia } from 'pinia'
import * as ElementPlusIconsVue from '@element-plus/icons-vue'
import { i18n } from './i18n'
import { applyTheme } from './theme'
import router from '@/router'

/** Installs every boot module (prototype: quasar boot files). */
export function bootApp(app: App) {
  for (const [key, component] of Object.entries(ElementPlusIconsVue)) app.component(key, component)
  app.use(createPinia())
  app.use(i18n)
  app.use(router)
  applyTheme()
  document.documentElement.lang = String(i18n.global.locale.value).slice(0, 2)
}
