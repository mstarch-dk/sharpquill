import { readdirSync } from 'node:fs'
import { fileURLToPath } from 'node:url'

// UI translations: every i18n/locales/<code>.json is a locale (code = file name, e.g. da, en, en-GB).
// Adding a language needs only a new file; the `language` site setting picks the active one (plugins/site-locale.ts)
const locales = readdirSync(fileURLToPath(new URL('./i18n/locales', import.meta.url)))
  .filter(file => file.endsWith('.json'))
  .sort()
  .map(file => ({ code: file.slice(0, -'.json'.length), language: file.slice(0, -'.json'.length), file }))

export default defineNuxtConfig({
  compatibilityDate: '2024-11-01',
  devtools: { enabled: true },

  extends: ['../layers/shared'],

  modules: ['@nuxtjs/tailwindcss', '@nuxtjs/i18n'],

  i18n: {
    locales,
    defaultLocale: 'da',
    // URLs stay unprefixed: a site has one language, chosen by the operator, not by the visitor's browser
    strategy: 'no_prefix',
    detectBrowserLanguage: false
  },

  css: ['~/assets/css/theme.css'],

  runtimeConfig: {
    // Overridden at runtime by NUXT_API_BASE_INTERNAL / NUXT_UPLOADS_PROXY / NUXT_PUBLIC_* env vars, never at build time
    apiBaseInternal: '',  // server-only, SSR requests inside the Docker network
    uploadsProxy: '',     // server-only, where /uploads/** is proxied to (dev only; empty = no proxy, Caddy serves it)
    public: {
      apiBase: 'http://localhost:5000',
      umamiHost: '',
      umamiWebsiteId: ''
    }
  },

  app: {
    head: {
      // Title, description and <html lang> come from the site settings (plugins/theme.server.ts)
      meta: [
        { charset: 'utf-8' },
        { name: 'viewport', content: 'width=device-width, initial-scale=1' }
      ],
      // Default favicon is the Sharpquill mark (assets/brand/); the pages themselves carry the site's own brand
      link: [
        { rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' },
        { rel: 'icon', type: 'image/png', sizes: '32x32', href: '/favicon-32.png' },
        { rel: 'apple-touch-icon', sizes: '180x180', href: '/apple-touch-icon.png' }
      ]
    }
  },

  typescript: {
    strict: true
  }
})
