import type { ApiResponse } from '~/types/api'
import type { ExportRequestDto, ImportValidationDto, ImportResultDto } from '~/types/export'

export interface ExportOptions {
  contentType?: string | null
  articleIds?: number[] | null
  includeTags?: boolean
  includeCategories?: boolean
  includeSettings?: boolean
}

export function useContentPackage() {
  const config = useRuntimeConfig()
  const api = useApi()
  // The export needs the raw Response for its blob, so it bypasses useApi and refreshes the token itself
  const { getAuthHeader, refreshToken, isTokenExpiringSoon } = useAuth()

  async function exportContent(options: ExportOptions = {}): Promise<Blob> {
    if (isTokenExpiringSoon()) {
      await refreshToken()
    }

    const baseUrl = config.public.apiBase
    const body: ExportRequestDto = {
      contentType: options.contentType,
      articleIds: options.articleIds,
      includeTags: options.includeTags,
      includeCategories: options.includeCategories,
      includeSettings: options.includeSettings
    }

    const response = await fetch(`${baseUrl}/api/admin/export`, {
      method: 'POST',
      headers: {
        ...getAuthHeader(),
        'Content-Type': 'application/json'
      },
      credentials: 'include',
      body: JSON.stringify(body)
    })

    if (!response.ok) {
      const error = await response.json()
      throw new Error(error?.error?.message || 'Export failed')
    }

    return response.blob()
  }

  async function validateImport(file: File): Promise<ApiResponse<ImportValidationDto>> {
    const formData = new FormData()
    formData.append('file', file)
    return api.post<ImportValidationDto>('/api/admin/import/validate', formData)
  }

  async function importContent(file: File): Promise<ApiResponse<ImportResultDto>> {
    const formData = new FormData()
    formData.append('file', file)
    return api.post<ImportResultDto>('/api/admin/import', formData)
  }

  function downloadBlob(blob: Blob, filename: string) {
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = filename
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
  }

  return {
    exportContent,
    validateImport,
    importContent,
    downloadBlob
  }
}
