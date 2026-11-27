import type { ApiResponse, ArticleDetailDto } from '~/types/api'

export function usePages() {
  const apiBase = useApiBase()
  const baseUrl = `${apiBase}/api/v1/pages`

  async function getPageBySlug(slug: string) {
    return useFetch<ApiResponse<ArticleDetailDto>>(`${baseUrl}/${slug}`, {
      key: `page-${slug}`
    })
  }

  return {
    getPageBySlug
  }
}
