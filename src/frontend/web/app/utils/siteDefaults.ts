// The public UI is Danish-first (see docs/frontend-uis.md). This is the language used when the `language` site
// setting has not been filled in. The fallback title and description for an empty site_name / site_description are
// translations (site.defaultTitle / site.defaultDescription in i18n/locales).
export const DEFAULT_LANGUAGE = 'da'

export interface PublicSiteSettings {
  siteName: string
  siteDescription: string
  language: string
  footerText: string
  // Footer contact column
  publicEmail: string
  publicPhone: string
  companyInfo: string
  socialLinkedIn: string
  socialTwitter: string
  socialFacebook: string
  socialInstagram: string
}

export function emptySiteSettings(): PublicSiteSettings {
  return {
    siteName: '', siteDescription: '', language: '', footerText: '',
    publicEmail: '', publicPhone: '', companyInfo: '',
    socialLinkedIn: '', socialTwitter: '', socialFacebook: '', socialInstagram: ''
  }
}

// Footer line: footer_text (with {year} replaced), else "© <year> <site name>", else "© <year>"
export function resolveFooterText(settings: PublicSiteSettings, year: number): string {
  const custom = settings.footerText.trim()
  if (custom) return custom.replaceAll('{year}', String(year))
  return settings.siteName.trim() ? `© ${year} ${settings.siteName.trim()}` : `© ${year}`
}
