import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vitest/config'
import { defineVitestProject } from '@nuxt/test-utils/config'
import { layerAliases, peerDependencies } from '../layers/shared/paths.ts'

// Date formatting depends on the time zone; pin it so results match on every machine and in CI
process.env.TZ = 'UTC'

export default defineConfig({
  test: {
    // `npm run test:coverage` (CI) measures both projects together. Scope: the TypeScript modules the tests target
    // (utils, composables, middleware, plugins); components and pages are left to end-to-end tests.
    coverage: {
      provider: 'v8',
      include: ['app/**/*.ts', '../themes/**/*.ts', '../layers/shared/app/**/*.ts'],
      // The shared themes and the shared layer live next to the app and are tested from here
      allowExternal: true,
      exclude: ['**/*.d.ts', 'app/types/**', '../layers/shared/app/types/**'],
      reporter: ['text-summary', 'json-summary', 'html'],
      reportsDirectory: 'coverage',
      // Ratchet: just below the measured coverage. Raise them when coverage grows; never lower them to pass CI.
      thresholds: {
        lines: 62,
        branches: 64
      }
    },
    projects: [
      // Pure helpers (app/utils, ../themes, the shared layer's utils). They run in plain Node without the Nuxt runtime,
      // so a module under test must import what it uses explicitly instead of relying on Nuxt auto-imports.
      {
        resolve: {
          alias: {
            '~': fileURLToPath(new URL('./app', import.meta.url)),
            ...layerAliases
          },
          // npm packages imported by the shared layer resolve from this app (layers/shared/paths.ts)
          dedupe: peerDependencies
        },
        test: {
          name: 'unit',
          environment: 'node',
          // The shared layer's tests run in both apps: each app builds the layer with its own dependencies
          include: ['test/unit/**/*.test.ts', '../layers/shared/test/unit/**/*.test.ts']
        }
      },
      // Composables and route middleware. They run inside a client-side Nuxt app (happy-dom), with auto-imports.
      await defineVitestProject({
        test: {
          name: 'nuxt',
          environment: 'nuxt',
          include: ['test/nuxt/**/*.test.ts', '../layers/shared/test/nuxt/**/*.test.ts'],
          environmentOptions: {
            nuxt: { domEnvironment: 'happy-dom' }
          }
        }
      })
    ]
  }
})
