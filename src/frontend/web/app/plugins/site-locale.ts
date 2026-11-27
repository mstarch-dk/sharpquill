import { DEFAULT_LANGUAGE } from '~/utils/siteDefaults'
import { resolveLocaleCode } from '~/utils/locale'

// Selects the UI translation from the `language` site setting. <html lang> keeps the setting's exact tag
// (plugins/site-head.ts); this only chooses which i18n/locales file the texts come from.
// Runs on the server after site-theme has loaded the settings, and again in the browser from the hydrated settings,
// before the app mounts, so both sides render the same locale.
export default defineNuxtPlugin({
  name: 'site-locale',
  dependsOn: ['site-theme'],
  async setup(nuxtApp) {
    const { language } = useSiteSettings()
    const i18n = nuxtApp.$i18n
    const available = i18n.localeCodes.value
    const fallback = available.find(code => code === DEFAULT_LANGUAGE) ?? i18n.locale.value
    const code = resolveLocaleCode(language.value, available, fallback)
    if (code !== i18n.locale.value) {
      await i18n.setLocale(code)
    }
  }
})
