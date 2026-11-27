import type { ApiResponse, SearchSuggestionDto } from '~/types/api'

const DEBOUNCE_MS = 150
const MIN_LENGTH = 2
const CACHE_SIZE = 50

// Per tab, browser only (never filled during SSR, so nothing is shared between visitors).
const cache = new Map<string, SearchSuggestionDto[]>()

/**
 * Search-as-you-type suggestions from GET /api/v1/search/suggest (answered from the API's in-memory index).
 * Requests are debounced, a newer query aborts the request in flight, and answers are cached per query and sign-in
 * state, so typing back and forth costs no requests.
 */
export function useSearchSuggestions() {
  const apiBase = useApiBase()
  const { authFetch, isAuthenticated } = useWebsiteAuth()

  const suggestions = ref<SearchSuggestionDto[]>([])
  const query = ref('')
  let timer: ReturnType<typeof setTimeout> | undefined
  let controller: AbortController | undefined

  function cancel() {
    clearTimeout(timer)
    controller?.abort()
    controller = undefined
  }

  function update(value: string) {
    cancel()
    const q = value.trim().slice(0, 100)
    if (q.length < MIN_LENGTH) {
      query.value = ''
      suggestions.value = []
      return
    }

    const key = `${isAuthenticated.value ? 'reader' : 'anon'}:${q.toLowerCase()}`
    const cached = cache.get(key)
    if (cached) {
      query.value = q
      suggestions.value = cached
      return
    }

    timer = setTimeout(async () => {
      const current = new AbortController()
      controller = current
      try {
        const response = await authFetch<ApiResponse<SearchSuggestionDto[]>>(`${apiBase}/api/v1/search/suggest`, {
          query: { q },
          signal: current.signal
        })
        if (current.signal.aborted) return
        cache.set(key, response.data)
        if (cache.size > CACHE_SIZE) cache.delete(cache.keys().next().value!)
        query.value = q
        suggestions.value = response.data
      } catch {
        // Suggestions are a convenience: on errors (or aborts) keep what is shown; submitting still searches.
      }
    }, DEBOUNCE_MS)
  }

  function clear() {
    cancel()
    query.value = ''
    suggestions.value = []
  }

  onBeforeUnmount(cancel)

  return { suggestions, query, update, clear }
}
