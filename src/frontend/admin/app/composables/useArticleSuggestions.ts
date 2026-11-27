import type { ArticleStatus } from '~/types/article'

export interface ArticleSuggestion {
  id: number
  title: string
  status: ArticleStatus
  publishedAt: string | null
}

const DEBOUNCE_MS = 150
const MIN_LENGTH = 2
const CACHE_SIZE = 50
const CACHE_TTL_MS = 30_000

/**
 * Jump-to suggestions for the article list from GET /api/admin/articles/suggest (the API's in-memory index, every
 * status). Debounced; a newer query aborts the request in flight. Answers are cached briefly, since titles and
 * statuses change while an author works.
 */
export function useArticleSuggestions() {
  const { get } = useApi()
  const suggestions = ref<ArticleSuggestion[]>([])
  const query = ref('')
  const cache = new Map<string, { at: number, data: ArticleSuggestion[] }>()
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

    const key = q.toLowerCase()
    const cached = cache.get(key)
    if (cached && Date.now() - cached.at < CACHE_TTL_MS) {
      query.value = q
      suggestions.value = cached.data
      return
    }

    timer = setTimeout(async () => {
      const current = new AbortController()
      controller = current
      try {
        const response = await get<ArticleSuggestion[]>('/api/admin/articles/suggest', { q, limit: 8 }, current.signal)
        if (current.signal.aborted) return
        cache.set(key, { at: Date.now(), data: response.data })
        if (cache.size > CACHE_SIZE) cache.delete(cache.keys().next().value!)
        query.value = q
        suggestions.value = response.data
      } catch {
        // A convenience only: the list filter keeps working.
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
