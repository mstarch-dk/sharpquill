import { layerAliases, peerDependencies, typePaths } from './paths'

// Shared Nuxt layer for the website and the admin (docs/frontend-uis.md, "Shared layer"). Both apps consume it with
// `extends: ['../layers/shared']`; its components, composables and utils auto-import as if they were the app's own.
export default defineNuxtConfig({
  alias: layerAliases,
  // npm packages imported by layer code resolve from the consuming app (paths.ts)
  vite: { resolve: { dedupe: peerDependencies } },
  modules: [
    // Runs per consuming app, so the paths point at that app's node_modules
    (_options, nuxt) => {
      nuxt.hook('prepare:types', ({ tsConfig, sharedTsConfig, serverTsConfig }) => {
        for (const config of [tsConfig, sharedTsConfig, serverTsConfig]) {
          Object.assign((config.compilerOptions ??= {}).paths ??= {}, typePaths(nuxt.options.rootDir))
        }
      })
    }
  ]
})
