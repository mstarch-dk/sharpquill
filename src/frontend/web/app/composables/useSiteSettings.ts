import { DEFAULT_LANGUAGE, emptySiteSettings, resolveFooterText } from '~/utils/siteDefaults'
import type { PublicSiteSettings } from '~/utils/siteDefaults'

// Site-wide branding settings, loaded once per request by plugins/theme.server.ts and shared via useState
export function useSiteSettings() {
  const settings = useState<PublicSiteSettings>('siteSettings', emptySiteSettings)
  const { $i18n } = useNuxtApp()

  const siteName = computed(() => settings.value.siteName.trim() || $i18n.t('site.defaultTitle'))
  const siteDescription = computed(() => settings.value.siteDescription.trim() || $i18n.t('site.defaultDescription'))
  const language = computed(() => settings.value.language.trim() || DEFAULT_LANGUAGE)
  const footerText = computed(() => resolveFooterText(settings.value, new Date().getFullYear()))

  return { settings, siteName, siteDescription, language, footerText }
}
