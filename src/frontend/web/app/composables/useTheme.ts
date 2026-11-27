import type { ThemeConfig, ThemeColors } from '../../../themes'
import { getThemeBySlug, getThemeColors, defaultThemeSlug } from '../../../themes'
import { getFontStackById, defaultFontId } from '../../../themes/fonts'

export function useTheme() {
  const apiBase = useApiBase()

  function colorsToCssVars(colors: ThemeColors, fonts: { heading: string; body: string }): Record<string, string> {
    return {
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
  }

  function themeTocssVars(theme: ThemeConfig, dark = false, fontId = defaultFontId): Record<string, string> {
    const colors = getThemeColors(theme, dark)
    const fontStack = getFontStackById(fontId)
    return colorsToCssVars(colors, { heading: fontStack, body: fontStack })
  }

  function themeToCSsVarsEntries(theme: ThemeConfig, dark = false, fontId = defaultFontId): [string, string][] {
    return Object.entries(themeTocssVars(theme, dark, fontId))
  }

  async function fetchSiteSettings(): Promise<{ themeSlug: string; darkMode: boolean; globalFont: string; loginEnabled: boolean }> {
    try {
      const response = await $fetch<{ data: { theme?: string; darkModeWeb?: boolean; globalFont?: string; loginEnabled?: boolean } }>(
        `${apiBase}/api/v1/settings`
      )
      return {
        themeSlug: response.data.theme || defaultThemeSlug,
        darkMode: response.data.darkModeWeb === true,
        globalFont: response.data.globalFont || defaultFontId,
        loginEnabled: response.data.loginEnabled !== false
      }
    } catch {
      return { themeSlug: defaultThemeSlug, darkMode: false, globalFont: defaultFontId, loginEnabled: true }
    }
  }

  async function fetchSiteThemeSlug(): Promise<string> {
    const settings = await fetchSiteSettings()
    return settings.themeSlug
  }

  function applyTheme(theme: ThemeConfig, dark = false, fontId = defaultFontId) {
    if (!import.meta.client) return
    const vars = themeTocssVars(theme, dark, fontId)
    const root = document.documentElement
    for (const [key, value] of Object.entries(vars)) {
      root.style.setProperty(key, value)
    }
  }

  return {
    themeTocssVars,
    themeToCSsVarsEntries,
    fetchSiteThemeSlug,
    fetchSiteSettings,
    applyTheme,
    getThemeBySlug,
  }
}
