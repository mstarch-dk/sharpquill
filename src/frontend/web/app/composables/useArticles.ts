import type { ApiResponse, ArticleListDto, ArticleDetailDto } from '~/types/api'

export function useArticles() {
  const apiBase = useApiBase()
  const baseUrl = `${apiBase}/api/v1/articles`

  async function getArticles(page = 1, pageSize = 10) {
    return useFetch<ApiResponse<ArticleListDto[]>>(baseUrl, {
      query: { page, pageSize },
      key: `articles-${page}-${pageSize}`
    })
  }

  async function getArticleBySlug(slug: string) {
    return useFetch<ApiResponse<ArticleDetailDto>>(`${baseUrl}/${slug}`, {
      key: `article-${slug}`
    })
  }

  async function getFeaturedArticles() {
    return useFetch<ApiResponse<ArticleListDto[]>>(`${baseUrl}/featured`, {
      key: 'articles-featured'
    })
  }

  return {
    getArticles,
    getArticleBySlug,
    getFeaturedArticles
  }
}
