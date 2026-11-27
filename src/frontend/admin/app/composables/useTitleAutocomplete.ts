export interface TitleAutocompleteResult {
  id: number
  title: string
  slug: string
  contentType: 'page' | 'article'
  url: string
}

export function useTitleAutocomplete() {
  const { get } = useApi()
  const results = ref<TitleAutocompleteResult[]>([])
  const loading = ref(false)
  let debounceTimer: ReturnType<typeof setTimeout> | null = null

  async function search(query: string) {
    if (!query || query.trim().length === 0) {
      results.value = []
      return
    }

    if (debounceTimer) clearTimeout(debounceTimer)
    debounceTimer = setTimeout(async () => {
      loading.value = true
      try {
        const response = await get<TitleAutocompleteResult[]>('/api/admin/title-autocomplete', { q: query, limit: 10 })
        results.value = response.data
      } catch {
        results.value = []
      } finally {
        loading.value = false
      }
    }, 300)
  }

  function clear() {
    results.value = []
    if (debounceTimer) clearTimeout(debounceTimer)
  }

  return { results, loading, search, clear }
}
