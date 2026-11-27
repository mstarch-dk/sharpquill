import type { ApiResponse, SearchResultDto } from '~/types/api'

export const SEARCH_PAGE_SIZE = 10

/**
 * Site search. Server-side the request is anonymous (readers' tokens live only in the browser); in the browser it is
 * sent with the reader's token, so signed-in readers also find members-only articles. The key includes the sign-in
 * state, so once the header's `initAuth()` restores a reader's session the anonymous SSR result is fetched again.
 */
export function useSearch(query: Ref<string>, page: Ref<number>) {
  const apiBase = useApiBase()
  const { authFetch, isAuthenticated } = useWebsiteAuth()
  const url = `${apiBase}/api/v1/search`

  return useAsyncData(
    () => `search-${query.value}-${page.value}-${isAuthenticated.value ? 'reader' : 'anon'}`,
    () => {
      if (!query.value) {
        return Promise.resolve<ApiResponse<SearchResultDto[]>>({
          data: [],
          meta: { page: 1, pageSize: SEARCH_PAGE_SIZE, totalCount: 0, totalPages: 0 }
        })
      }
      const options = { query: { q: query.value, page: page.value, pageSize: SEARCH_PAGE_SIZE } }
      return import.meta.client
        ? authFetch<ApiResponse<SearchResultDto[]>>(url, options)
        : $fetch<ApiResponse<SearchResultDto[]>>(url, options)
    }
  )
}
