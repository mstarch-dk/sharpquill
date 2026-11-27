export default defineNuxtConfig({
  compatibilityDate: '2024-11-01',
  devtools: { enabled: true },
  ssr: false,
  extends: ['../layers/shared'],
  modules: ['@nuxtjs/tailwindcss'],
  css: ['~/assets/css/main.css'],
  app: {
    head: {
      // Sharpquill favicon set (sources and palette in assets/brand/)
      link: [
        { rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' },
        { rel: 'icon', type: 'image/png', sizes: '32x32', href: '/favicon-32.png' },
        { rel: 'icon', type: 'image/png', sizes: '16x16', href: '/favicon-16.png' },
        { rel: 'apple-touch-icon', sizes: '180x180', href: '/apple-touch-icon.png' },
        { rel: 'manifest', href: '/site.webmanifest' }
      ],
      meta: [{ name: 'theme-color', content: '#0F3D3E' }]
    }
  },
  // Overridden at runtime by NUXT_UPLOADS_PROXY / NUXT_PUBLIC_* env vars, never at build time
  runtimeConfig: {
    uploadsProxy: 'http://localhost:5000',  // server-only, where /uploads/** is proxied to
    public: {
      apiBase: 'http://localhost:5000',
      webBase: 'http://localhost:3000'
    }
  },
  typescript: {
    strict: true
  }
})
