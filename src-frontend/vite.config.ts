import { fileURLToPath, URL } from 'node:url'

import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import AutoImport from 'unplugin-auto-import/vite'
import Components from 'unplugin-vue-components/vite'
import { ElementPlusResolver } from 'unplugin-vue-components/resolvers'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const apiTarget = env.VITE_API_PROXY_TARGET || 'http://localhost:5190'
  // Vitest runs with mode 'test': skip the per-component SCSS imports (Node cannot load .scss and tests never render styles).
  const importStyle = mode === 'test' ? false : ('sass' as const)

  return {
    plugins: [
      vue(),
      // Auto-import Vue / Router / Pinia APIs and Element Plus components on demand.
      AutoImport({
        imports: ['vue', 'vue-router', 'pinia', 'vue-i18n'],
        resolvers: [ElementPlusResolver({ importStyle })],
        dts: 'src/types/auto-imports.d.ts',
        eslintrc: { enabled: false },
      }),
      Components({
        resolvers: [ElementPlusResolver({ importStyle })],
        dts: 'src/types/components.d.ts',
      }),
    ],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    css: {
      preprocessorOptions: {
        scss: {
          api: 'modern-compiler',
          // Inject the Element Plus theme override into every SCSS chunk.
          additionalData: `@use "@/styles/element/index.scss" as *;`,
        },
      },
    },
    server: {
      port: 5174,
      proxy: {
        '/api': { target: apiTarget, changeOrigin: true },
        '/files': { target: apiTarget, changeOrigin: true },
      },
    },
  }
})
