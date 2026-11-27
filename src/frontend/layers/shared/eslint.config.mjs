import { resolve } from 'node:path'
import { pathToFileURL } from 'node:url'

// ESLint only lints files below the directory it runs in, and the layer has no ESLint install of its own. So an
// app's `npm run lint` changes into the layer and runs its own ESLint with ESLINT_APP_DIR pointing back at the app:
// this config is that app's config (whose plugins resolve from the app's node_modules).
const appDir = process.env.ESLINT_APP_DIR
if (!appDir) {
  throw new Error('Lint the layer through an app: npm run lint in src/frontend/web or src/frontend/admin')
}

const appConfig = await import(pathToFileURL(resolve(appDir, 'eslint.config.mjs')).href)

export default appConfig.default
