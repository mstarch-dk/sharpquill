import type { ApiResponse, LikeStatusDto } from '~/types/api'

const VISITOR_ID_KEY = 'cms_visitor_id'

function getOrCreateVisitorId(): string {
  if (import.meta.server) return ''
  let id = localStorage.getItem(VISITOR_ID_KEY)
  if (!id) {
    id = crypto.randomUUID()
    localStorage.setItem(VISITOR_ID_KEY, id)
  }
  return id
}

export function useLikes() {
  const apiBase = useApiBase()

  function getLikesUrl(articleId: number) {
    return `${apiBase}/api/v1/articles/${articleId}/likes`
  }

  async function getLikeStatus(articleId: number) {
    const visitorId = getOrCreateVisitorId()
    const url = visitorId
      ? `${getLikesUrl(articleId)}?visitorId=${visitorId}`
      : getLikesUrl(articleId)
    return $fetch<ApiResponse<LikeStatusDto>>(url)
  }

  async function toggleLike(articleId: number) {
    const visitorId = getOrCreateVisitorId()
    return $fetch<ApiResponse<LikeStatusDto>>(getLikesUrl(articleId), {
      method: 'POST',
      body: { visitorId }
    })
  }

  return {
    getLikeStatus,
    toggleLike,
    getOrCreateVisitorId
  }
}
