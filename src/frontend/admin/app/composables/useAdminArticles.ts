import type { ApiResponse } from '~/types/api'
import type {
  ArticleAdminDto,
  ArticleCreateDto,
  ArticleUpdateDto,
  ArticleVersionDto,
  ArticleVersionDetailDto,
  ArticlePreviewDto,
  ArticleListFilters
} from '~/types/article'

export function useAdminArticles() {
  const api = useApi()
  const endpoint = '/api/admin/articles'

  async function getArticles(
    page = 1,
    pageSize = 10,
    filters: ArticleListFilters = {}
  ): Promise<ApiResponse<ArticleAdminDto[]>> {
    // Unset filters are left out of the query string.
    const query: Record<string, unknown> = { page, pageSize }
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== null && value !== '') query[key] = value
    }
    return api.get<ArticleAdminDto[]>(endpoint, query)
  }

  async function getArticle(id: number): Promise<ApiResponse<ArticleAdminDto>> {
    return api.get<ArticleAdminDto>(`${endpoint}/${id}`)
  }

  async function createArticle(data: ArticleCreateDto): Promise<ApiResponse<ArticleAdminDto>> {
    return api.post<ArticleAdminDto>(endpoint, data)
  }

  async function updateArticle(
    id: number,
    data: ArticleUpdateDto
  ): Promise<ApiResponse<ArticleAdminDto>> {
    return api.put<ArticleAdminDto>(`${endpoint}/${id}`, data)
  }

  async function deleteArticle(id: number): Promise<ApiResponse<void>> {
    return api.del<void>(`${endpoint}/${id}`)
  }

  async function getVersions(articleId: number): Promise<ApiResponse<ArticleVersionDto[]>> {
    return api.get<ArticleVersionDto[]>(`${endpoint}/${articleId}/versions`)
  }

  async function getVersion(articleId: number, versionId: number): Promise<ApiResponse<ArticleVersionDetailDto>> {
    return api.get<ArticleVersionDetailDto>(`${endpoint}/${articleId}/versions/${versionId}`)
  }

  async function publishVersion(articleId: number, versionId: number): Promise<ApiResponse<ArticleAdminDto>> {
    return api.put<ArticleAdminDto>(`${endpoint}/${articleId}/versions/${versionId}/publish`, {})
  }

  async function getPreview(id: number): Promise<ApiResponse<ArticlePreviewDto>> {
    return api.get<ArticlePreviewDto>(`${endpoint}/${id}/preview`)
  }

  return {
    getArticles,
    getArticle,
    createArticle,
    updateArticle,
    deleteArticle,
    getVersions,
    getVersion,
    publishVersion,
    getPreview
  }
}
