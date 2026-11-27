import type { ApiResponse, CategoryDto, ArticleListDto } from '~/types/api'

export function useCategories() {
  const apiBase = useApiBase()
  const baseUrl = `${apiBase}/api/v1/categories`

  async function getCategories() {
    return useFetch<ApiResponse<CategoryDto[]>>(baseUrl, {
      key: 'categories'
    })
  }

  async function getCategoryArticles(slug: string, page = 1, pageSize = 10) {
    return useFetch<ApiResponse<ArticleListDto[]>>(`${baseUrl}/${slug}/articles`, {
      query: { page, pageSize },
      key: `category-${slug}-articles-${page}-${pageSize}`
    })
  }

  return {
    getCategories,
    getCategoryArticles
  }
}
