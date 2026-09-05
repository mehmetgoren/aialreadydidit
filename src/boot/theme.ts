import { ref, watch } from 'vue'
import { localService, Themes } from '@/utils/services/local-service'

/** Element Plus dark mode: toggles `html.dark`, persisted in LocalService. */
export const isDark = ref(localService.getTheme() === Themes.Dark)

export function applyTheme() {
  document.documentElement.classList.toggle('dark', isDark.value)
}

watch(isDark, (v) => {
  localService.setTheme(v ? Themes.Dark : Themes.Light)
  applyTheme()
})

export function toggleDark(value?: boolean) {
  isDark.value = typeof value === 'boolean' ? value : !isDark.value
}
