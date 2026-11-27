import type { ApiResponse } from '~/types/api'
import type {
  SeriesDto,
  SeriesAdminDetailDto,
  SeriesCreateDto,
  SeriesUpdateDto,
  SeriesAddArticleDto,
  SeriesReorderDto
} from '~/types/series'

export function useAdminSeries() {
  const api = useApi()
  const endpoint = '/api/admin/series'

  async function getSeries(): Promise<ApiResponse<SeriesDto[]>> {
    return api.get<SeriesDto[]>(endpoint)
  }

  async function getSeriesById(id: number): Promise<ApiResponse<SeriesAdminDetailDto>> {
    return api.get<SeriesAdminDetailDto>(`${endpoint}/${id}`)
  }

  async function createSeries(data: SeriesCreateDto): Promise<ApiResponse<SeriesDto>> {
    return api.post<SeriesDto>(endpoint, data)
  }

  async function updateSeries(id: number, data: SeriesUpdateDto): Promise<ApiResponse<SeriesDto>> {
    return api.put<SeriesDto>(`${endpoint}/${id}`, data)
  }

  async function deleteSeries(id: number): Promise<ApiResponse<void>> {
    return api.del<void>(`${endpoint}/${id}`)
  }

  async function addArticleToSeries(seriesId: number, data: SeriesAddArticleDto): Promise<ApiResponse<void>> {
    return api.post<void>(`${endpoint}/${seriesId}/articles`, data)
  }

  async function removeArticleFromSeries(seriesId: number, articleId: number): Promise<ApiResponse<void>> {
    return api.del<void>(`${endpoint}/${seriesId}/articles/${articleId}`)
  }

  async function reorderArticles(seriesId: number, data: SeriesReorderDto): Promise<ApiResponse<void>> {
    return api.put<void>(`${endpoint}/${seriesId}/articles/reorder`, data)
  }

  return {
    getSeries,
    getSeriesById,
    createSeries,
    updateSeries,
    deleteSeries,
    addArticleToSeries,
    removeArticleFromSeries,
    reorderArticles
  }
}
