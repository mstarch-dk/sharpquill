import type { ApiResponse } from '~/types/api'

export interface SiteSettingsDto {
  siteName?: string
  siteDescription?: string
  siteUrl?: string
  contactEmail?: string
  socialTwitter?: string
  socialFacebook?: string
  socialInstagram?: string
  socialLinkedIn?: string
  metaKeywords?: string
  theme?: string
  globalFont?: string
  language?: string
  footerText?: string
  darkModeAdmin?: boolean
  darkModeWeb?: boolean
  loginEnabled?: boolean
  publicEmail?: string
  publicPhone?: string
  companyInfo?: string
}

export interface SiteSettingsUpdateDto {
  siteName?: string
  siteDescription?: string
  siteUrl?: string
  contactEmail?: string
  socialTwitter?: string
  socialFacebook?: string
  socialInstagram?: string
  socialLinkedIn?: string
  metaKeywords?: string
  theme?: string
  globalFont?: string
  language?: string
  footerText?: string
  darkModeAdmin?: boolean
  darkModeWeb?: boolean
  loginEnabled?: boolean
  publicEmail?: string
  publicPhone?: string
  companyInfo?: string
}

export function useAdminSettings() {
  const api = useApi()
  const endpoint = '/api/admin/settings'

  async function getSettings(): Promise<ApiResponse<SiteSettingsDto>> {
    return api.get<SiteSettingsDto>(endpoint)
  }

  async function updateSettings(data: SiteSettingsUpdateDto): Promise<ApiResponse<SiteSettingsDto>> {
    return api.put<SiteSettingsDto>(endpoint, data)
  }

  return {
    getSettings,
    updateSettings
  }
}
