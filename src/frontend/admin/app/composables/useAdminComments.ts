import type { ApiResponse } from '~/types/api'
import type { CommentAdminDto, CommentCountsDto } from '~/types/comment'
import { CommentStatus } from '~/types/comment'

export function useAdminComments() {
  const api = useApi()
  const endpoint = '/api/admin/comments'

  async function getComments(params: {
    page?: number
    pageSize?: number
    status?: CommentStatus | null
  } = {}): Promise<ApiResponse<CommentAdminDto[]>> {
    const query = new URLSearchParams()
    if (params.page) query.set('page', params.page.toString())
    if (params.pageSize) query.set('pageSize', params.pageSize.toString())
    if (params.status !== undefined && params.status !== null) query.set('status', params.status.toString())
    const qs = query.toString()
    return api.get<CommentAdminDto[]>(`${endpoint}${qs ? `?${qs}` : ''}`)
  }

  async function getComment(id: number): Promise<ApiResponse<CommentAdminDto>> {
    return api.get<CommentAdminDto>(`${endpoint}/${id}`)
  }

  async function getCounts(): Promise<ApiResponse<CommentCountsDto>> {
    return api.get<CommentCountsDto>(`${endpoint}/counts`)
  }

  async function moderateComment(id: number, status: CommentStatus): Promise<void> {
    await api.put<void>(`${endpoint}/${id}/moderate`, { status })
  }

  async function deleteComment(id: number): Promise<void> {
    await api.del<void>(`${endpoint}/${id}`)
  }

  return {
    getComments,
    getComment,
    getCounts,
    moderateComment,
    deleteComment
  }
}
