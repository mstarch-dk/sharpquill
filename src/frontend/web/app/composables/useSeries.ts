import type { ApiResponse, SeriesDto, SeriesDetailDto } from '~/types/api'

export function useSeries() {
  const apiBase = useApiBase()
  const baseUrl = `${apiBase}/api/v1/series`

  async function getAllSeries() {
    return useFetch<ApiResponse<SeriesDto[]>>(baseUrl, {
      key: 'series-all'
    })
  }

  async function getSeriesBySlug(slug: string) {
    return useFetch<ApiResponse<SeriesDetailDto>>(`${baseUrl}/${slug}`, {
      key: `series-${slug}`
    })
  }

  return {
    getAllSeries,
    getSeriesBySlug
  }
}
