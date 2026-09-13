<script setup lang="ts">
import { computed } from 'vue'
import { platformIcon } from '@/utils/tools'

/**
 * Platform glyph. Windows and macOS get real logo silhouettes (paths from the CC0 Simple Icons set) because the
 * emoji alternatives are wrong: 🪟 is a house window and is missing on Windows 10, 🍎 is a fruit. The other platforms
 * keep their emoji. Windows is drawn in Windows blue; a parent can override the fill through `--platform-icon-fill`
 * (the primary download button sets it to currentColor so the logo stays white on blue).
 */
const props = defineProps<{ code: string | null | undefined }>()
const kind = computed(() => (props.code === 'windows' ? 'windows' : props.code === 'macos' ? 'apple' : 'emoji'))
</script>

<template>
  <svg v-if="kind === 'windows'" class="platform-icon platform-icon--windows" viewBox="0 0 24 24" aria-hidden="true">
    <path d="M0 3.449L9.75 2.1v9.451H0m10.949-9.602L24 0v11.4H10.949M0 12.6h9.75v9.451L0 20.699M10.949 12.6H24V24l-12.9-1.801" />
  </svg>
  <svg v-else-if="kind === 'apple'" class="platform-icon platform-icon--apple" viewBox="0 0 24 24" aria-hidden="true">
    <path d="M12.152 6.896c-.948 0-2.415-1.078-3.96-1.04-2.04.027-3.91 1.183-4.961 3.014-2.117 3.675-.546 9.103 1.519 12.09 1.013 1.454 2.208 3.09 3.792 3.039 1.52-.065 2.09-.987 3.935-.987 1.831 0 2.35.987 3.96.948 1.637-.026 2.676-1.48 3.676-2.948 1.156-1.688 1.636-3.325 1.662-3.415-.039-.013-3.182-1.221-3.22-4.857-.026-3.04 2.48-4.494 2.597-4.559-1.429-2.09-3.623-2.324-4.39-2.376-2-.156-3.675 1.09-4.61 1.09zM15.53 3.83c.843-1.012 1.4-2.427 1.245-3.83-1.207.052-2.662.805-3.532 1.818-.78.896-1.454 2.338-1.273 3.714 1.338.104 2.715-.688 3.559-1.701" />
  </svg>
  <span v-else class="platform-icon platform-icon--emoji" aria-hidden="true">{{ platformIcon(code) }}</span>
</template>

<style scoped>
.platform-icon {
  display: inline-block;
  width: 1em;
  height: 1em;
  vertical-align: -0.125em;
  line-height: 1;
}
.platform-icon--windows {
  fill: var(--platform-icon-fill, #0078d4);
}
.platform-icon--apple {
  fill: var(--platform-icon-fill, currentColor);
}
.platform-icon--emoji {
  width: auto;
  height: auto;
  vertical-align: baseline;
}
</style>
