import type { ApiResponse, CommentDto } from '~/types/api'

export function useComments() {
  const apiBase = useApiBase()
  const { getAuthHeader, authFetch } = useWebsiteAuth()

  function getCommentsUrl(articleId: number) {
    return `${apiBase}/api/v1/articles/${articleId}/comments`
  }

  async function getComments(articleId: number) {
    const { data, refresh } = await useFetch<ApiResponse<CommentDto[]>>(
      getCommentsUrl(articleId),
      // Comments of members-only articles require the member's token
      { key: `comments-${articleId}`, headers: getAuthHeader() }
    )
    return { data, refresh }
  }

  async function createComment(articleId: number, payload: {
    authorName: string
    authorEmail: string
    content: string
    parentCommentId?: number
    honeypot?: string
  }) {
    return authFetch<ApiResponse<CommentDto>>(getCommentsUrl(articleId), {
      method: 'POST',
      body: payload
    })
  }

  return {
    getComments,
    createComment
  }
}
