import { createRequire } from 'node:module'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'

// The layer is not an installed package: it has no dependencies or node_modules of its own (its package.json only
// marks the config files as ES modules). npm packages its code imports are dependencies of each consuming app (both
// package.json files list them) and must resolve from that app's node_modules, whereas Node, Vite and TypeScript
// would look next to the layer:
// - Vite (app build, Vitest) resolves them from the app root through `resolve.dedupe`. The import stays a bare
//   specifier, so the server build traces the package into .output like any other dependency. (An alias to the
//   app's node_modules would bake the build machine's absolute paths into the server bundle.)
// - TypeScript gets a path to the app's copy (typePaths).
export const peerDependencies = ['xss']

// Imported only for types: layer files use types from vue, the layer's tests import vitest and
// @nuxt/test-utils/runtime. At runtime Nuxt and Vitest provide them, so they need a TypeScript path only.
export const typeOnlyDependencies = ['vue', 'vitest', '@nuxt/test-utils/runtime']

// The layer's app/ directory, imported as `#layer/...` (types and utils; components and composables auto-import)
export const layerAppDir = fileURLToPath(new URL('./app', import.meta.url))

export const layerAliases: Record<string, string> = { '#layer': layerAppDir }

// TypeScript paths to the app's copy of every dependency above: a package name maps to the package directory
// (TypeScript reads its package.json), a subpath to the file the app's Node resolution picks (TypeScript finds the
// .d.mts next to it)
export function typePaths(appRootDir: string): Record<string, string[]> {
  const require = createRequire(join(appRootDir, 'package.json'))
  return Object.fromEntries([...peerDependencies, ...typeOnlyDependencies].map((specifier) => {
    const isPackageName = specifier.split('/').length === (specifier.startsWith('@') ? 2 : 1)
    return [specifier, [isPackageName ? join(appRootDir, 'node_modules', specifier) : require.resolve(specifier)]]
  }))
}
