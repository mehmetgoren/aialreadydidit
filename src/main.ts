import { createApp } from 'vue'
import App from './App.vue'
import { bootApp } from './boot'
import './styles/index.scss'

// After a deploy an already-open shell may reference chunk hashes that no longer exist; reload once to pick up the new build.
window.addEventListener('vite:preloadError', (event) => {
  const key = 'aadi_reload_after_preload_error'
  if (sessionStorage.getItem(key) === location.href) return // already retried for this URL: let the error surface
  sessionStorage.setItem(key, location.href)
  event.preventDefault()
  location.reload()
})

const app = createApp(App)
bootApp(app).then(() => app.mount('#app'))
