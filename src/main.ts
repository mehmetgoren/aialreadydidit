import { createApp } from 'vue'
import App from './App.vue'
import { bootApp } from './boot'
import './styles/index.scss'

const app = createApp(App)
bootApp(app)
app.mount('#app')
