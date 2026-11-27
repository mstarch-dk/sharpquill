import type { ApiResponse } from '~/types/api'
import type {
  ArticleAdminDto,
  ArticleCreateDto,
  ArticlePreviewDto,
  ArticleUpdateDto
} from '~/types/article'

export function useAdminPages() {
  const api = useApi()
  const endpoint = '/api/admin/pages'

  async function getPages(page = 1, pageSize = 50): Promise<ApiResponse<ArticleAdminDto[]>> {
    return api.get<ArticleAdminDto[]>(endpoint, { page, pageSize })
  }

  async function getPageById(id: number): Promise<ApiResponse<ArticleAdminDto>> {
    return api.get<ArticleAdminDto>(`${endpoint}/${id}`)
  }

  async function createPage(data: ArticleCreateDto): Promise<ApiResponse<ArticleAdminDto>> {
    return api.post<ArticleAdminDto>(endpoint, data)
  }

  async function updatePage(id: number, data: ArticleUpdateDto): Promise<ApiResponse<ArticleAdminDto>> {
    return api.put<ArticleAdminDto>(`${endpoint}/${id}`, data)
  }

  async function deletePage(id: number): Promise<ApiResponse<void>> {
    return api.del<void>(`${endpoint}/${id}`)
  }

  async function getPreview(id: number): Promise<ApiResponse<ArticlePreviewDto>> {
    return api.get<ArticlePreviewDto>(`${endpoint}/${id}/preview`)
  }

  return {
    getPages,
    getPageById,
    createPage,
    updatePage,
    deletePage,
    getPreview
  }
}
