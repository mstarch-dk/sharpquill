import type { ApiResponse, TagDto, ArticleListDto } from '~/types/api'

export function useTags() {
  const apiBase = useApiBase()
  const baseUrl = `${apiBase}/api/v1/tags`

  async function getTags() {
    return useFetch<ApiResponse<TagDto[]>>(baseUrl, {
      key: 'tags'
    })
  }

  async function getTagArticles(slug: string, page = 1, pageSize = 10) {
    return useFetch<ApiResponse<ArticleListDto[]>>(`${baseUrl}/${slug}/articles`, {
      query: { page, pageSize },
      key: `tag-${slug}-articles-${page}-${pageSize}`
    })
  }

  return {
    getTags,
    getTagArticles
  }
}
