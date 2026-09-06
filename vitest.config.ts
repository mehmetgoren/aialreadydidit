import { fileURLToPath } from 'node:url'
import { configDefaults, defineConfig, mergeConfig } from 'vitest/config'
import viteConfig from './vite.config.ts'

/** Unit tests: jsdom + the same Vite plugins (auto-imports, Element Plus resolver) as the app. */
export default defineConfig((env) =>
  mergeConfig(
    viteConfig(env),
    defineConfig({
      test: {
        environment: 'jsdom',
        include: ['src/**/__tests__/*.spec.ts'],
        exclude: [...configDefaults.exclude, 'e2e/**'],
        root: fileURLToPath(new URL('./', import.meta.url)),
        restoreMocks: true,
        unstubEnvs: true,
        coverage: {
          provider: 'v8',
          include: ['src/utils/**', 'src/stores/**', 'src/boot/**', 'src/router/**', 'src/components/common/**', 'src/pages/catalog/use-app-browser.ts'],
        },
      },
    }),
  ),
)
