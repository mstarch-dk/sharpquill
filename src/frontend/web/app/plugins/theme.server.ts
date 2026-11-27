import { getThemeBySlug, getThemeColors, defaultThemeSlug } from '../../../themes'
import { getFontStackById } from '../../../themes/fonts'
import type { ThemeColors } from '../../../themes'
import type { PublicSiteSettings } from '~/utils/siteDefaults'

function colorsToCssString(colors: ThemeColors, fonts: { heading: string; body: string }): string {
  const vars: Record<string, string> = {
    '--color-primary-50': colors.primary[50],
    '--color-primary-100': colors.primary[100],
    '--color-primary-200': colors.primary[200],
    '--color-primary-300': colors.primary[300],
    '--color-primary-400': colors.primary[400],
    '--color-primary-500': colors.primary[500],
    '--color-primary-600': colors.primary[600],
    '--color-primary-700': colors.primary[700],
    '--color-primary-800': colors.primary[800],
    '--color-primary-900': colors.primary[900],
    '--color-primary-950': colors.primary[950],
    '--color-background': colors.background,
    '--color-surface': colors.surface,
    '--color-text': colors.text,
    '--color-text-secondary': colors.textSecondary,
    '--font-heading': fonts.heading,
    '--font-body': fonts.body,
  }
  return Object.entries(vars).map(([k, v]) => `${k}:${v}`).join(';')
}

export default defineNuxtPlugin({
  name: 'site-theme',
  async setup() {
    const config = useRuntimeConfig()
    const { settings: siteSettings } = useSiteSettings()

    let themeSlug = defaultThemeSlug
    let darkMode = false
    let fontId = 'inter'
    let fontStack = getFontStackById()
    try {
      const response = await $fetch<{ data: { theme?: string; darkModeWeb?: boolean; globalFont?: string } & Partial<Record<keyof PublicSiteSettings, string | null>> }>(
        `${config.apiBaseInternal || config.public.apiBase}/api/v1/settings`
      )
      siteSettings.value = {
        siteName: response.data.siteName || '',
        siteDescription: response.data.siteDescription || '',
        language: response.data.language || '',
        footerText: response.data.footerText || '',
        publicEmail: response.data.publicEmail || '',
        publicPhone: response.data.publicPhone || '',
        companyInfo: response.data.companyInfo || '',
        socialLinkedIn: response.data.socialLinkedIn || '',
        socialTwitter: response.data.socialTwitter || '',
        socialFacebook: response.data.socialFacebook || '',
        socialInstagram: response.data.socialInstagram || ''
      }
      themeSlug = response.data.theme || defaultThemeSlug
      darkMode = response.data.darkModeWeb === true
      fontId = response.data.globalFont || 'inter'
      fontStack = getFontStackById(response.data.globalFont)
    } catch {
      // Fall back to defaults
    }

    // Hydrate theme state so the client can toggle dark mode without re-fetching
    useState('siteThemeSlug', () => themeSlug)
    useState('siteDarkMode', () => darkMode)
    useState('siteGlobalFont', () => fontId)

    const theme = getThemeBySlug(themeSlug)
    const colors = getThemeColors(theme, darkMode)
    const cssString = colorsToCssString(colors, { heading: fontStack, body: fontStack })

    // Title, description, og:site_name and lang are set by the universal plugins/site-head.ts.
    // The dark class stays server-only: useDarkMode toggles it on <html> directly in the browser.
    useHead({
      htmlAttrs: darkMode ? { class: 'dark' } : {},
      style: [
        { innerHTML: `:root{${cssString}}` }
      ]
    })
  }
})
