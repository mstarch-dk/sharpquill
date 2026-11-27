import type { Config } from 'tailwindcss'
import typography from '@tailwindcss/typography'

export default {
  darkMode: 'class',
  content: [
    './app/components/**/*.{js,vue,ts}',
    './app/layouts/**/*.vue',
    './app/pages/**/*.vue',
    './app/plugins/**/*.{js,ts}',
    './app/app.vue',
    './app/error.vue',
    // The shared layer's components and block renderers/editors (nuxt.config.ts extends ../layers/shared)
    '../layers/shared/app/**/*.{js,vue,ts}'
  ],
  theme: {
    extend: {
      colors: {
        // Sharpquill "Ink teal" (assets/brand/README.md): 300 light accent, 600 accent, 900 primary, 950 dark background
        primary: {
          50: '#effaf8',
          100: '#d7f1ec',
          200: '#b0e2da',
          300: '#7cc4b8',
          400: '#4aa89b',
          500: '#2c8e82',
          600: '#1f7a72',
          700: '#1a6560',
          800: '#16524f',
          900: '#0f3d3e',
          950: '#0a2324'
        }
      },
      fontFamily: {
        heading: 'var(--font-heading)',
        body: 'var(--font-body)',
      }
    }
  },
  plugins: [typography]
} satisfies Config
